using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyPortfolio.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed; // Thư viện Cache
using System.Text.Json; // Thư viện xử lý JSON

namespace MyPortfolio.Web.Pages.Admin
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache; // Gọi dịch vụ Cache

        public DashboardModel(ApplicationDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public string[] SongTitles { get; set; } = Array.Empty<string>();
        public int[] PlayCounts { get; set; } = Array.Empty<int>();
        public int TotalSongs { get; set; }
        public int TotalPlays { get; set; }

        public async Task OnGetAsync()
        {
            string cacheKey = "dashboard_stats";

            // 1. Thử lấy dữ liệu từ Redis trước
            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                Console.WriteLine("========== 🟢 ĐÃ LẤY TỪ REDIS CACHE SIÊU TỐC! ==========");
                // ✅ HIT CACHE: Có dữ liệu rồi -> Dùng luôn (Siêu nhanh)
                var stats = JsonSerializer.Deserialize<DashboardStats>(cachedData);
                SongTitles = stats.Titles;
                PlayCounts = stats.Counts;
                TotalSongs = stats.TotalSongs;
                TotalPlays = stats.TotalPlays;
            }
            else
            {
                Console.WriteLine("========== 🔴 KHÔNG CÓ CACHE, ĐANG GỌI DATABASE NEON... ==========");
                // ❌ MISS CACHE: Chưa có -> Phải gọi Database (Chậm hơn)
                var songs = await _context.PortfolioItems
                    .Select(p => new { p.Title, p.PlayCount })
                    .OrderByDescending(p => p.PlayCount) // Sắp xếp giảm dần
                    .Take(10) // Chỉ lấy Top 10
                    .ToListAsync();

                SongTitles = songs.Select(s => s.Title ?? "Untitled").ToArray();
                PlayCounts = songs.Select(s => s.PlayCount).ToArray();
                TotalSongs = await _context.PortfolioItems.CountAsync();
                TotalPlays = songs.Sum(s => s.PlayCount); // Lưu ý: Đây là tổng của Top 10, muốn tổng hết phải query riêng

                // Lưu vào Redis để lần sau dùng (Hết hạn sau 10 phút)
                var dataToCache = new DashboardStats
                {
                    Titles = SongTitles,
                    Counts = PlayCounts,
                    TotalSongs = TotalSongs,
                    TotalPlays = TotalPlays
                };

                var options = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(10)); // Cache sống 10 phút

                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dataToCache), options);
            }
        }

        // Class phụ để lưu dữ liệu
        public class DashboardStats
        {
            public string[] Titles { get; set; }
            public int[] Counts { get; set; }
            public int TotalSongs { get; set; }
            public int TotalPlays { get; set; }
        }
    }
}