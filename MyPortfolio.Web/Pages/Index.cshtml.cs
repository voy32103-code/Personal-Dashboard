using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyPortfolio.Core.Entities;
using MyPortfolio.Infrastructure.Data;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace MyPortfolio.Web.Pages
{
    [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;

        public IndexModel(ApplicationDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public IList<PortfolioItem> Projects { get; set; } = new List<PortfolioItem>();

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Mode { get; set; }

        public async Task OnGetAsync()
        {
            string modeKey = string.IsNullOrEmpty(Mode) ? "normal" : Mode.ToLower();
            string searchKey = string.IsNullOrEmpty(SearchString) ? "none" : SearchString.ToLower().Trim();
            string cacheKey = $"home_projects_{modeKey}_{searchKey}";

            // 🐛 FIX BUG 4: Set Title ở đầu hàm, không bị phụ thuộc vào block if-else của Redis
            ViewData["Title"] = (modeKey == "library") ? "Thư Viện Của Tôi" : "Trang Chủ";

            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                // 🐛 FIX BUG 2: Bọc try-catch để Deserialization an toàn
                try
                {
                    Projects = JsonSerializer.Deserialize<List<PortfolioItem>>(cachedData);
                    if (Projects == null) throw new InvalidOperationException("Deserialization failed");

                    Console.WriteLine($"========== 🟢 TRANG CHỦ: LẤY TỪ REDIS [{cacheKey}] ==========");
                    return; // Lấy cache thành công thì dừng tại đây
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Lỗi đọc Cache: {ex.Message}. Đang xóa cache lỗi...");
                    await _cache.RemoveAsync(cacheKey); // Xóa cục rác trong Redis
                    Projects = new List<PortfolioItem>();
                }
            }

            // Gọi Database nếu không có cache hoặc cache bị hỏng
            Console.WriteLine($"========== 🔴 TRANG CHỦ: GỌI DATABASE [{cacheKey}] ==========");

            var query = _context.PortfolioItems.AsQueryable();

            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(s => s.Title.ToLower().Contains(SearchString.ToLower())
                                      || (s.Artist != null && s.Artist.ToLower().Contains(SearchString.ToLower()))
                                      || s.Description.ToLower().Contains(SearchString.ToLower()));
            }

            if (modeKey == "library")
            {
                query = query.Where(s => s.IsFavorite == true);
            }

            Projects = await query.OrderByDescending(s => s.CreatedDate).ToListAsync();

            if (Projects.Any())
            {
                var options = new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(Projects), options);
            }
        }

        // 🐛 FIX BUG 1 & 3: Helper Method để xóa sạch các cache liên quan
        private async Task InvalidateProjectsCache()
        {
            // Ghi chú: IDistributedCache mặc định của .NET KHÔNG hỗ trợ xóa theo pattern (dấu *).
            // Để hệ thống không bị crash và không cần cài thêm thư viện phức tạp,
            // chúng ta sẽ xóa các key phổ biến nhất, bao gồm cả key tìm kiếm hiện tại (nếu có).
            var keysToRemove = new[]
            {
                "home_projects_normal_none",
                "home_projects_library_none",
                $"home_projects_normal_{SearchString?.ToLower().Trim() ?? "none"}"
            };

            foreach (var key in keysToRemove.Distinct())
            {
                await _cache.RemoveAsync(key);
            }
        }

        public async Task<IActionResult> OnPostToggleHeartAsync(int id)
        {
            var item = await _context.PortfolioItems.FindAsync(id);

            if (item != null)
            {
                item.IsFavorite = !item.IsFavorite;
                await _context.SaveChangesAsync();

                // 🐛 FIX BUG 1 & 3: Gọi hàm xóa toàn bộ Cache
                await InvalidateProjectsCache();

                return new JsonResult(new { success = true, isFavorite = item.IsFavorite });
            }

            return new JsonResult(new { success = false });
        }

        public async Task<IActionResult> OnPostCountPlayAsync(int id)
        {
            var item = await _context.PortfolioItems.FindAsync(id);
            if (item != null)
            {
                item.PlayCount++;
                await _context.SaveChangesAsync();

                // 🐛 FIX BUG 1 & 3
                await InvalidateProjectsCache();
            }
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnGetCountPlayAsync(int id)
        {
            var song = await _context.PortfolioItems.FindAsync(id);
            if (song != null)
            {
                song.PlayCount++;
                await _context.SaveChangesAsync();

                // 🐛 FIX BUG 1 & 3
                await InvalidateProjectsCache();

                return new JsonResult(new { success = true, newCount = song.PlayCount });
            }
            return new JsonResult(new { success = false });
        }
    }
}