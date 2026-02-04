using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyPortfolio.Core.Entities;
using MyPortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MyPortfolio.Web.Pages.Portfolio
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public PortfolioItem PortfolioItem { get; set; } = default!;

        // 1. Lấy thông tin chi tiết bài hát khi vào trang
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var portfolioItem = await _context.PortfolioItems.FirstOrDefaultAsync(m => m.Id == id);

            if (portfolioItem == null)
            {
                return NotFound();
            }

            PortfolioItem = portfolioItem;
            return Page();
        }

        // 2. Xử lý khi bấm nút Thả Tim (MỚI THÊM)
        public async Task<IActionResult> OnPostToggleHeartAsync(int id)
        {
            var item = await _context.PortfolioItems.FindAsync(id);

            if (item != null)
            {
                // Đảo ngược trạng thái: Thích <-> Không thích
                item.IsFavorite = !item.IsFavorite;
                await _context.SaveChangesAsync();
            }

            // Quan trọng: Redirect lại trang này để load lại dữ liệu mới nhất
            return RedirectToPage(new { id = id });
        }
    }
}