using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyPortfolio.Core.Entities;
using MyPortfolio.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
namespace MyPortfolio.Web.Pages.Portfolio
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment; // Cần cái này để lưu file

        public EditModel(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public PortfolioItem PortfolioItem { get; set; } = default!;

        // Biến hứng file ảnh mới (nếu có)
        [BindProperty]
        public IFormFile? ImageUpload { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var item = await _context.PortfolioItems.FindAsync(id);
            if (item == null) return NotFound();

            PortfolioItem = item;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            // --- LOGIC UPLOAD ẢNH (EDIT) ---
            if (ImageUpload != null)
            {
                // 1. Nếu người dùng chọn ảnh mới -> Upload và ghi đè
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageUpload.FileName);
                var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", fileName);

                using (var fileStream = new FileStream(uploadPath, FileMode.Create))
                {
                    await ImageUpload.CopyToAsync(fileStream);
                }

                // Cập nhật đường dẫn ảnh mới
                PortfolioItem.ImageUrl = "/uploads/" + fileName;
            }
            // Nếu ImageUpload == null thì nó sẽ tự giữ lại cái Link cũ 
            // (nhờ vào cái input hidden bên file giao diện)
            // -------------------------------

            // Fix lỗi ngày giờ UTC
            PortfolioItem.CreatedDate = DateTime.SpecifyKind(PortfolioItem.CreatedDate, DateTimeKind.Utc);

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