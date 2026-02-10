using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyPortfolio.Core.Entities;
using MyPortfolio.Infrastructure.Data;

namespace MyPortfolio.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<PortfolioItem> Projects { get; set; } = new List<PortfolioItem>();

        // --- KHAI BÁO BIẾN GIAO DIỆN ---

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; } // Biến tìm kiếm

        [BindProperty(SupportsGet = true)]
        public string? Mode { get; set; } // Biến chế độ (Library/Home)

        // --------------------------------

        public async Task OnGetAsync()
        {
            var query = _context.PortfolioItems.AsQueryable();

            // 1. Xử lý Tìm kiếm
            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(s => s.Title.ToLower().Contains(SearchString.ToLower())
                                      || (s.Artist != null && s.Artist.ToLower().Contains(SearchString.ToLower()))
                                      || s.Description.ToLower().Contains(SearchString.ToLower()));
            }

            // 2. Xử lý Thư viện (Chỉ hiện bài đã thích)
            if (Mode == "library")
            {
                query = query.Where(s => s.IsFavorite == true);
                ViewData["Title"] = "Thư Viện Của Tôi";
            }
            else
            {
                ViewData["Title"] = "Trang Chủ";
            }

            // Lấy dữ liệu và sắp xếp bài mới nhất lên đầu
            Projects = await query.OrderByDescending(s => s.CreatedDate).ToListAsync();
        }

        // 3. Xử lý Thả Tim
        public async Task<IActionResult> OnPostToggleHeartAsync(int id)
        {
            var item = await _context.PortfolioItems.FindAsync(id);

            if (item != null)
            {
                item.IsFavorite = !item.IsFavorite;
                await _context.SaveChangesAsync();

                // TRẢ VỀ JSON THAY VÌ REDIRECT
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
            }
            return new JsonResult(new { success = true });
        }
        // Trong file Pages/Index.cshtml.cs

        public async Task<IActionResult> OnGetCountPlayAsync(int id)
        {
            var song = await _context.PortfolioItems.FindAsync(id);
            if (song != null)
            {
                song.PlayCount++; // Tăng lượt nghe
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true, newCount = song.PlayCount });
            }
            return new JsonResult(new { success = false });
        }
    }
}