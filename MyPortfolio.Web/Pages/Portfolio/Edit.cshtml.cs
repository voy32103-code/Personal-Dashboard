using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyPortfolio.Core.Entities;
using MyPortfolio.Infrastructure.Data;

namespace MyPortfolio.Web.Pages.Portfolio
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public PortfolioItem PortfolioItem { get; set; } = default!;

        // 1. Khi mở trang: Lấy dữ liệu cũ lên để hiển thị
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var item = await _context.PortfolioItems.FindAsync(id);
            if (item == null) return NotFound();

            PortfolioItem = item;
            return Page();
        }

        // 2. Khi bấm Save: Lưu thay đổi đè vào cái cũ
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();
            PortfolioItem.CreatedDate = DateTime.SpecifyKind(PortfolioItem.CreatedDate, DateTimeKind.Utc);
            // Báo cho Database biết là dòng này bị thay đổi rồi
            _context.Attach(PortfolioItem).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                if (!_context.PortfolioItems.Any(e => e.Id == PortfolioItem.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("/Index");
        }
    }
}