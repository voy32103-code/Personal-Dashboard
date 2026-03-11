using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyPortfolio.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace MyPortfolio.Web.Pages.Admin
{
    [Authorize]
    // Fix: XÓA [ResponseCache] ở class level — tương tự bug ở ProfileModel
    // Dashboard là trang admin, không nên cache response HTTP
    // Dùng Redis cache cho data thay thế
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<DashboardModel> _logger;

        private const int TOP_SONGS_COUNT = 10;
        private const int CACHE_DURATION_MINUTES = 10;
        private const string CACHE_KEY = "admin_dashboard_stats_v2"; // bump version khi thay đổi schema

        public DashboardModel(ApplicationDbContext context, IDistributedCache cache, ILogger<DashboardModel> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        // --- DỮ LIỆU BÀI HÁT ---
        public string[] SongTitles { get; set; } = Array.Empty<string>();
        public int[] PlayCounts { get; set; } = Array.Empty<int>();
        public int TotalSongs { get; set; }
        public int TotalPlays { get; set; }

        // --- DỮ LIỆU ANALYTICS ---
        public int TotalDownloads { get; set; }
        public int TotalQrScans { get; set; }
        public string ChartLabelsJson { get; set; } = "[]";
        public string DownloadDataJson { get; set; } = "[]";
        public string ScanDataJson { get; set; } = "[]";
        public List<LogItem> RecentLogs { get; set; } = new();

        public class LogItem
        {
            public string Action { get; set; } = string.Empty;
            public string IPAddress { get; set; } = string.Empty;
            public DateTime Time { get; set; }
        }

        public async Task OnGetAsync()
        {
            // =====================================================
            // 1. THỬ LẤY TOÀN BỘ STATS TỪ CACHE
            // =====================================================
            var cachedData = await _cache.GetStringAsync(CACHE_KEY);

            if (!string.IsNullOrEmpty(cachedData))
            {
                try
                {
                    var stats = JsonSerializer.Deserialize<DashboardStats>(cachedData);
                    if (stats != null)
                    {
                        ApplyStats(stats);
                        _logger.LogInformation("🟢 Dashboard: lấy từ Redis [{Key}]", CACHE_KEY);

                        // Analytics data (download/scan/chart) luôn lấy fresh — thay đổi liên tục
                        await LoadAnalyticsAsync();
                        return;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning("❌ Cache lỗi: {Msg}. Đang xóa...", ex.Message);
                    await _cache.RemoveAsync(CACHE_KEY);
                }
            }

            // =====================================================
            // 2. CACHE MISS → QUERY DATABASE
            // Top Trending = tổng lượt nghe (PlayCount) mỗi project
            // =====================================================
            _logger.LogInformation("🔴 Dashboard: gọi DB [{Key}]", CACHE_KEY);

            TotalSongs = await _context.PortfolioItems.AsNoTracking().CountAsync();

            if (TotalSongs > 0)
            {
                // Tính tổng lượt nghe toàn bộ
                TotalPlays = await _context.PortfolioItems
                    .AsNoTracking()
                    .SumAsync(p => p.PlayCount);

                // Top Trending: sắp xếp theo PlayCount giảm dần
                var topSongs = await _context.PortfolioItems
                    .AsNoTracking()
                    .Where(p => p.PlayCount > 0) // Chỉ lấy bài có lượt nghe
                    .OrderByDescending(p => p.PlayCount)
                    .Take(TOP_SONGS_COUNT)
                    .Select(p => new { p.Title, p.PlayCount })
                    .ToListAsync();

                // Fallback nếu tất cả PlayCount = 0
                if (!topSongs.Any())
                {
                    topSongs = await _context.PortfolioItems
                        .AsNoTracking()
                        .OrderByDescending(p => p.CreatedDate)
                        .Take(TOP_SONGS_COUNT)
                        .Select(p => new { p.Title, p.PlayCount })
                        .ToListAsync();
                }

                SongTitles = topSongs.Select(s => s.Title ?? "Untitled").ToArray();
                PlayCounts = topSongs.Select(s => s.PlayCount).ToArray();
            }
            else
            {
                SongTitles = new[] { "Chưa có dữ liệu" };
                PlayCounts = new[] { 0 };
            }

            // Lưu song stats vào cache
            var statsToCache = new DashboardStats
            {
                Titles = SongTitles,
                Counts = PlayCounts,
                TotalSongs = TotalSongs,
                TotalPlays = TotalPlays
            };

            await _cache.SetStringAsync(
                CACHE_KEY,
                JsonSerializer.Serialize(statsToCache),
                new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES))
            );

            // Analytics luôn lấy fresh (không cache vì thay đổi liên tục)
            await LoadAnalyticsAsync();
        }

        // =====================================================
        // LOAD ANALYTICS: CV Downloads, QR Scans, Chart, Logs
        // Tách riêng để dễ gọi lại sau khi dùng cache song stats
        // =====================================================
        private async Task LoadAnalyticsAsync()
        {
            TotalDownloads = await _context.DownloadLogs.CountAsync();
            TotalQrScans = await _context.QrScanLogs.CountAsync();

            // Biểu đồ 7 ngày qua
            var startDate = DateTime.UtcNow.AddDays(-6).Date;

            var downloads = await _context.DownloadLogs
                .Where(d => d.DownloadedAt >= startDate)
                .GroupBy(d => d.DownloadedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var scans = await _context.QrScanLogs
                .Where(s => s.ScannedAt >= startDate)
                .GroupBy(s => s.ScannedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var labels = new List<string>();
            var downloadCounts = new List<int>();
            var scanCounts = new List<int>();

            for (int i = 0; i <= 6; i++)
            {
                var date = startDate.AddDays(i);
                labels.Add(date.ToString("dd/MM"));
                downloadCounts.Add(downloads.FirstOrDefault(d => d.Date == date)?.Count ?? 0);
                scanCounts.Add(scans.FirstOrDefault(s => s.Date == date)?.Count ?? 0);
            }

            ChartLabelsJson = JsonSerializer.Serialize(labels);
            DownloadDataJson = JsonSerializer.Serialize(downloadCounts);
            ScanDataJson = JsonSerializer.Serialize(scanCounts);

            // 5 log gần nhất (kết hợp Download + QR)
            var recentDownloads = await _context.DownloadLogs
                .OrderByDescending(d => d.DownloadedAt)
                .Take(5)
                .Select(d => new LogItem
                {
                    Action = "Download CV",
                    IPAddress = d.IPAddress ?? "Unknown",
                    Time = d.DownloadedAt
                })
                .ToListAsync();

            var recentScans = await _context.QrScanLogs
                .OrderByDescending(s => s.ScannedAt)
                .Take(5)
                .Select(s => new LogItem
                {
                    Action = "Scan QR",
                    IPAddress = s.IPAddress ?? "Unknown",
                    Time = s.ScannedAt
                })
                .ToListAsync();

            RecentLogs = recentDownloads
                .Concat(recentScans)
                .OrderByDescending(l => l.Time)
                .Take(5)
                .ToList();
        }

        // Helper: gán stats từ cache object sang properties
        private void ApplyStats(DashboardStats stats)
        {
            SongTitles = stats.Titles;
            PlayCounts = stats.Counts;
            TotalSongs = stats.TotalSongs;
            TotalPlays = stats.TotalPlays;
        }

        // =====================================================
        // CACHE SCHEMA — bump CACHE_KEY khi thay đổi
        // =====================================================
        public class DashboardStats
        {
            public string[] Titles { get; init; } = Array.Empty<string>();
            public int[] Counts { get; init; } = Array.Empty<int>();
            public int TotalSongs { get; init; }
            public int TotalPlays { get; init; }
        }
    }
}