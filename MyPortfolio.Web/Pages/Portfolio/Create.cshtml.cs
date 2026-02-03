using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyPortfolio.Core.Entities;
using MyPortfolio.Infrastructure.Data;

namespace MyPortfolio.Web.Pages.Portfolio
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        // Code này để kết nối Database
        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Tạo biến để hứng dữ liệu từ Form nhập
        [BindProperty]
        public PortfolioItem PortfolioItem { get; set; } = default!;

        public IActionResult OnGet()
        {
            return Page();
        }

        // Hàm này chạy khi người dùng bấm nút "Save"
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Lưu vào Database
            _context.PortfolioItems.Add(PortfolioItem);
            await _context.SaveChangesAsync();

            // Lưu xong thì quay về trang chủ
            return RedirectToPage("/Index");
        }
    }
}