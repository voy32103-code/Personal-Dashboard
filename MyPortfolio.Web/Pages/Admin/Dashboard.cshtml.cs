using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyPortfolio.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MyPortfolio.Web.Pages.Admin
{
    // Yêu cầu quyền Admin (Nếu hệ thống chưa có Role, tạm thời có thể bỏ (Roles = "Admin") đi và chỉ dùng [Authorize])
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<DashboardModel> _logger; //  Dùng Logger thay vì Console

        //  Không dùng Magic Numbers, định nghĩa rõ Constant và Cache Key có Namespace
        private const int TOP_SONGS_COUNT = 10;
        private const int CACHE_DURATION_MINUTES = 10;
        private const string CACHE_KEY = "admin_dashboard_stats_v1";

        public DashboardModel(
            ApplicationDbContext context,
            IDistributedCache cache,
            ILogger<DashboardModel> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public string[] SongTitles { get; set; } = Array.Empty<string>();
        public int[] PlayCounts { get; set; } = Array.Empty<int>();
        public int TotalSongs { get; set; }
        public int TotalPlays { get; set; }

        public async Task OnGetAsync()
        {
            var cachedData = await _cache.GetStringAsync(CACHE_KEY);

            if (!string.IsNullOrEmpty(cachedData))
            {
                //  Bắt lỗi Deserialization
                try
                {
                    var stats = JsonSerializer.Deserialize<DashboardStats>(cachedData);

                    if (stats != null && stats.Titles != null && stats.Counts != null)
                    {
                        _logger.LogInformation("✅ [DASHBOARD] Lấy dữ liệu từ Redis Cache thành công.");
                        SongTitles = stats.Titles;
                        PlayCounts = stats.Counts;
                        TotalSongs = stats.TotalSongs;
                        TotalPlays = stats.TotalPlays;
                        return;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "❌ [DASHBOARD] Cache JSON bị lỗi định dạng. Đang xóa rác trong Redis...");
                    await _cache.RemoveAsync(CACHE_KEY);
                }
            }

            _logger.LogInformation("⚠️ [DASHBOARD] Không có Cache hợp lệ. Đang truy xuất Database...");

            // Dùng AsNoTracking() cho tác vụ Read-Only
            TotalSongs = await _context.PortfolioItems.AsNoTracking().CountAsync();

            // Xử lý giao diện khi Database chưa có bài hát nào (Empty State)
            if (TotalSongs == 0)
            {
                SongTitles = new[] { "Chưa có dữ liệu" };
                PlayCounts = new[] { 0 };
                TotalPlays = 0;
                _logger.LogInformation("ℹ️ [DASHBOARD] Database hiện tại đang trống.");
                return; // Trống thì không cần lưu Cache làm gì
            }

            // Tính tổng lượt nghe của TẤT CẢ BÀI HÁT 
            TotalPlays = await _context.PortfolioItems
                .AsNoTracking()
                .SumAsync(p => p.PlayCount);

            // Truy vấn lấy bảng xếp hạng Top Bài Hát
            var topSongs = await _context.PortfolioItems
                .AsNoTracking()
                .Select(p => new { p.Title, p.PlayCount })
                .OrderByDescending(p => p.PlayCount)
               .Take(TOP_SONGS_COUNT)
               .ToListAsync();

            SongTitles = topSongs.Select(s => s.Title ?? "Untitled").ToArray();
            PlayCounts = topSongs.Select(s => s.PlayCount).ToArray();

            // Đóng gói dữ liệu để lưu vào Redis
            var dataToCache = new DashboardStats
            {
                Titles = SongTitles,
                Counts = PlayCounts,
                TotalSongs = TotalSongs,
                TotalPlays = TotalPlays
            };

            var options = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

            await _cache.SetStringAsync(CACHE_KEY, JsonSerializer.Serialize(dataToCache), options);
            _logger.LogInformation($"✅ [DASHBOARD] Đã lưu dữ liệu vào Redis Cache (Sống {CACHE_DURATION_MINUTES} phút).");
        }

        //  Thiết kế class an toàn hơn với thuộc tính `init` (bất biến sau khi tạo)
        public class DashboardStats
        {
            public string[] Titles { get; init; } = Array.Empty<string>();
            public int[] Counts { get; init; } = Array.Empty<int>();
            public int TotalSongs { get; init; }
            public int TotalPlays { get; init; }
        }
    }
}