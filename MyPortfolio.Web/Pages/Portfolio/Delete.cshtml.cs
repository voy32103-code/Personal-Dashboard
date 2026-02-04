using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyPortfolio.Core.Entities;
using MyPortfolio.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;

namespace MyPortfolio.Web.Pages.Portfolio
{
    [Authorize]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment; 

        public DeleteModel(ApplicationDbContext context, IWebHostEnvironment environment) 
        {
            _context = context;
            _environment = environment; 
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
        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var item = await _context.PortfolioItems.FindAsync(id);
            if (item != null)
            {
                // Xóa file ảnh
                if (!string.IsNullOrEmpty(item.ImageUrl) && !item.ImageUrl.Contains("placeholder"))
                {
                    var imagePath = Path.Combine(_environment.WebRootPath, item.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                // Xóa file audio
                if (!string.IsNullOrEmpty(item.AudioUrl))
                {
                    var audioPath = Path.Combine(_environment.WebRootPath, item.AudioUrl.TrimStart('/'));
                    if (System.IO.File.Exists(audioPath))
                    {
                        System.IO.File.Delete(audioPath);
                    }
                }

                _context.PortfolioItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/Index");
        }
    }
}