using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyPortfolio.Core.Entities;
using MyPortfolio.Infrastructure.Data;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace MyPortfolio.Web.Pages
{
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

        public async Task<IActionResult> OnGetAsync()
        {
            string modeKey = string.IsNullOrEmpty(Mode) ? "normal" : Mode.ToLower();
            string searchKey = string.IsNullOrEmpty(SearchString) ? "none" : SearchString.ToLower().Trim();
            string cacheKey = $"home_projects:{modeKey}:{searchKey}";

            ViewData["Title"] = (modeKey == "library") ? "Thư Viện Của Tôi" : "Trang Chủ";

            // --- 1. THỬ LẤY TỪ CACHE ---
            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                try
                {
                    var cached = JsonSerializer.Deserialize<List<PortfolioItem>>(cachedData);
                    if (cached == null) throw new InvalidOperationException("Deserialization trả về null.");

                    Projects = cached;
                    Console.WriteLine($"========== 🟢 TRANG CHỦ: LẤY TỪ REDIS [{cacheKey}] ==========");
                    return Page();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Lỗi đọc Cache: {ex.Message}. Đang xóa cache lỗi...");
                    await _cache.RemoveAsync(cacheKey);
                }
            }

            // --- 2. GỌI DATABASE ---
            Console.WriteLine($"========== 🔴 TRANG CHỦ: GỌI DATABASE [{cacheKey}] ==========");

            try
            {
                var query = _context.PortfolioItems.AsQueryable();

                if (!string.IsNullOrEmpty(SearchString))
                {
                    var keyword = SearchString.ToLower();
                    query = query.Where(s =>
                        s.Title.ToLower().Contains(keyword) ||
                        (s.Artist != null && s.Artist.ToLower().Contains(keyword)) ||
                        s.Description.ToLower().Contains(keyword));
                }

                if (modeKey == "library")
                    query = query.Where(s => s.IsFavorite == true);

                Projects = await query
                    .OrderByDescending(s => s.CreatedDate)
                    .ToListAsync();

                // Chỉ cache khi có dữ liệu
                if (Projects.Any())
                {
                    var options = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));

                    await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(Projects), options);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi truy vấn DB: {ex.Message}");
                Projects = new List<PortfolioItem>();
                TempData["ErrorMessage"] = "Không thể tải dữ liệu. Vui lòng thử lại sau.";
            }

            return Page();
        }

        // --- 3. XÓA CACHE SAU KHI THAY ĐỔI DỮ LIỆU ---
        private async Task InvalidateProjectsCache()
        {
            var keysToRemove = new[]
            {
                "home_projects:normal:none",
                "home_projects:library:none",
                string.IsNullOrEmpty(SearchString) ? null : $"home_projects:normal:{SearchString.ToLower().Trim()}",
                string.IsNullOrEmpty(SearchString) ? null : $"home_projects:library:{SearchString.ToLower().Trim()}"
            };

            foreach (var key in keysToRemove.Where(k => k != null).Distinct())
            {
                await _cache.RemoveAsync(key!);
            }
        }

        // --- 4. TOGGLE TIM (POST) ---
        public async Task<IActionResult> OnPostToggleHeartAsync(int id)
        {
            var item = await _context.PortfolioItems.FindAsync(id);

            if (item == null)
                return new JsonResult(new { success = false, message = "Không tìm thấy item." });

            item.IsFavorite = !item.IsFavorite;
            await _context.SaveChangesAsync();
            await InvalidateProjectsCache();

            return new JsonResult(new { success = true, isFavorite = item.IsFavorite });
        }

        // --- 5. ĐẾM LƯỢT NGHE (POST) ---
        public async Task<IActionResult> OnPostCountPlayAsync(int id)
        {
            var song = await _context.PortfolioItems.FindAsync(id);

            if (song == null)
                return new JsonResult(new { success = false, message = "Không tìm thấy bài hát." });

            song.PlayCount++;
            await _context.SaveChangesAsync();
            await InvalidateProjectsCache();

            return new JsonResult(new { success = true, newCount = song.PlayCount });
        }
    }
}