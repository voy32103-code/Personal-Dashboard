using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyPortfolio.Core.Entities;
using MyPortfolio.Infrastructure.Data;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using MyPortfolio.Web.Infrastructure;

namespace MyPortfolio.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, IDistributedCache cache, ILogger<IndexModel> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
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
            // H-1: Dùng CacheKeys.HomeProjects() — đồng bộ với Create/Edit/Delete
            string cacheKey = CacheKeys.HomeProjects(modeKey, searchKey);

            ViewData["Title"] = (modeKey == "library") ? "Thư Viện Của Tôi" : "Trang Chủ";

            // --- 1. THử LẤY TỪ CACHE ---
            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                try
                {
                    var cached = JsonSerializer.Deserialize<List<PortfolioItem>>(cachedData);
                    if (cached == null) throw new InvalidOperationException("Deserialization trả về null.");

                    Projects = cached;
                    // M-1: Dùng ILogger thay vì Console.WriteLine
                    _logger.LogInformation("Cache HIT: {CacheKey}", cacheKey);
                    return Page();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cache read failed for key: {CacheKey}. Removing stale cache.", cacheKey);
                    await _cache.RemoveAsync(cacheKey);
                }
            }

            // --- 2. GỌI DATABASE ---
            _logger.LogInformation("Cache MISS, querying DB: {CacheKey}", cacheKey);

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
                _logger.LogError(ex, "DB query failed for key: {CacheKey}", cacheKey);
                Projects = new List<PortfolioItem>();
                TempData["ErrorMessage"] = "Không thể tải dữ liệu. Vui lòng thử lại sau.";
            }

            return Page();
        }

        // H-1: Dùng CacheKeys constants — đồng bộ format với Create/Edit/Delete
        private async Task InvalidateProjectsCache()
        {
            var keysToRemove = new[]
            {
                CacheKeys.HomeProjectsNormal,
                CacheKeys.HomeProjectsLibrary,
                string.IsNullOrEmpty(SearchString) ? null : CacheKeys.HomeProjects("normal", SearchString.ToLower().Trim()),
                string.IsNullOrEmpty(SearchString) ? null : CacheKeys.HomeProjects("library", SearchString.ToLower().Trim())
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
            var affectedRows = await _context.PortfolioItems
                .Where(p => p.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.PlayCount, p => p.PlayCount + 1));

            if (affectedRows == 0)
                return new JsonResult(new { success = false, message = "Không tìm thấy bài hát." });

            var newCount = await _context.PortfolioItems
                .Where(p => p.Id == id)
                .Select(p => p.PlayCount)
                .FirstOrDefaultAsync();

            await InvalidateProjectsCache();

            return new JsonResult(new { success = true, newCount = newCount });
        }
    }
}