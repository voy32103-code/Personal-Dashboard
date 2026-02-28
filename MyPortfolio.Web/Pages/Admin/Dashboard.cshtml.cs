using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyPortfolio.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MyPortfolio.Web.Pages.Admin
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<DashboardModel> _logger;

        private const int TOP_SONGS_COUNT = 10;
        private const int CACHE_DURATION_MINUTES = 10;
        private const string CACHE_KEY = "admin_dashboard_stats_v1";

        public DashboardModel(ApplicationDbContext context, IDistributedCache cache, ILogger<DashboardModel> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        // --- DỮ LIỆU BÀI HÁT (CŨ) ---
        public string[] SongTitles { get; set; } = Array.Empty<string>();
        public int[] PlayCounts { get; set; } = Array.Empty<int>();
        public int TotalSongs { get; set; }
        public int TotalPlays { get; set; }

        // --- DỮ LIỆU ANALYTICS MỚI (THÊM VÀO) ---
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
            // ==========================================
            // 1. XỬ LÝ DỮ LIỆU BÀI HÁT (CÓ CACHE)
            // ==========================================
            var cachedData = await _cache.GetStringAsync(CACHE_KEY);
            if (!string.IsNullOrEmpty(cachedData))
            {
                try
                {
                    var stats = JsonSerializer.Deserialize<DashboardStats>(cachedData);
                    if (stats != null && stats.Titles != null && stats.Counts != null)
                    {
                        SongTitles = stats.Titles;
                        PlayCounts = stats.Counts;
                        TotalSongs = stats.TotalSongs;
                        TotalPlays = stats.TotalPlays;
                    }
                }
                catch (JsonException)
                {
                    await _cache.RemoveAsync(CACHE_KEY);
                }
            }

            if (TotalSongs == 0) // Nếu cache miss hoặc trống
            {
                TotalSongs = await _context.PortfolioItems.AsNoTracking().CountAsync();
                if (TotalSongs > 0)
                {
                    TotalPlays = await _context.PortfolioItems.AsNoTracking().SumAsync(p => p.PlayCount);
                    var topSongs = await _context.PortfolioItems.AsNoTracking()
                        .Select(p => new { p.Title, p.PlayCount })
                        .OrderByDescending(p => p.PlayCount).Take(TOP_SONGS_COUNT).ToListAsync();

                    SongTitles = topSongs.Select(s => s.Title ?? "Untitled").ToArray();
                    PlayCounts = topSongs.Select(s => s.PlayCount).ToArray();

                    var dataToCache = new DashboardStats { Titles = SongTitles, Counts = PlayCounts, TotalSongs = TotalSongs, TotalPlays = TotalPlays };
                    await _cache.SetStringAsync(CACHE_KEY, JsonSerializer.Serialize(dataToCache), new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES)));
                }
                else
                {
                    SongTitles = new[] { "Chưa có dữ liệu" };
                    PlayCounts = new[] { 0 };
                }
            }

            // ==========================================
            // 2. XỬ LÝ DỮ LIỆU ANALYTICS (CV & QR CODE)
            // ==========================================
            TotalDownloads = await _context.DownloadLogs.CountAsync();
            TotalQrScans = await _context.QrScanLogs.CountAsync();

            // Lấy data 7 ngày qua cho biểu đồ Line Chart
            var startDate = DateTime.UtcNow.AddDays(-6).Date;

            var downloads = await _context.DownloadLogs
                .Where(d => d.DownloadedAt >= startDate)
                .GroupBy(d => d.DownloadedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() }).ToListAsync();

            var scans = await _context.QrScanLogs
                .Where(s => s.ScannedAt >= startDate)
                .GroupBy(s => s.ScannedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() }).ToListAsync();

            var labels = new List<string>();
            var downloadCounts = new List<int>();
            var scanCounts = new List<int>();

            for (int i = 0; i <= 6; i++)
            {
                var currentDate = startDate.AddDays(i);
                labels.Add(currentDate.ToString("dd/MM"));
                downloadCounts.Add(downloads.FirstOrDefault(d => d.Date == currentDate)?.Count ?? 0);
                scanCounts.Add(scans.FirstOrDefault(s => s.Date == currentDate)?.Count ?? 0);
            }

            ChartLabelsJson = JsonSerializer.Serialize(labels);
            DownloadDataJson = JsonSerializer.Serialize(downloadCounts);
            ScanDataJson = JsonSerializer.Serialize(scanCounts);

            // Lấy 5 log gần nhất
            var recentDownloads = await _context.DownloadLogs.OrderByDescending(d => d.DownloadedAt).Take(5)
                .Select(d => new LogItem { Action = "Download CV", IPAddress = d.IPAddress ?? "Unknown", Time = d.DownloadedAt }).ToListAsync();
            var recentScans = await _context.QrScanLogs.OrderByDescending(s => s.ScannedAt).Take(5)
                .Select(s => new LogItem { Action = "Scan QR", IPAddress = s.IPAddress ?? "Unknown", Time = s.ScannedAt }).ToListAsync();

            RecentLogs = recentDownloads.Concat(recentScans).OrderByDescending(l => l.Time).Take(5).ToList();
        }

        public class DashboardStats
        {
            public string[] Titles { get; init; } = Array.Empty<string>();
            public int[] Counts { get; init; } = Array.Empty<int>();
            public int TotalSongs { get; init; }
            public int TotalPlays { get; init; }
        }
    }
}