using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyPortfolio.Core.Entities;
using MyPortfolio.Infrastructure.Data;

namespace MyPortfolio.Web.Pages.Portfolio
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public PortfolioItem PortfolioItem { get; set; } = default!;

        // 1. Mở trang: Lấy thông tin dự án lên để người dùng xem lại lần cuối
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var item = await _context.PortfolioItems.FindAsync(id);

            if (item == null) return NotFound();

            PortfolioItem = item;
            return Page();
        }

        // 2. Bấm nút Xóa thật: Thực hiện xóa khỏi Database
        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var item = await _context.PortfolioItems.FindAsync(id);
            if (item != null)
            {
                // Lệnh xóa cực đơn giản
                _context.PortfolioItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/Index");
        }
    }
}