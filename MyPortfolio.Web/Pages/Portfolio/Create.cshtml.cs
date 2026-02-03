using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyPortfolio.Core.Entities;
using MyPortfolio.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;

namespace MyPortfolio.Web.Pages.Portfolio
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment; // Dịch vụ để lấy đường dẫn file

        public CreateModel(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public PortfolioItem PortfolioItem { get; set; } = default!;

        // Biến này để hứng file ảnh từ Form
        [BindProperty]
        public IFormFile? ImageUpload { get; set; }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            // --- LOGIC UPLOAD ẢNH ---
            if (ImageUpload != null)
            {
                // 1. Tạo tên file ngẫu nhiên để không bị trùng
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageUpload.FileName);

                // 2. Lấy đường dẫn đến thư mục wwwroot/uploads
                var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", fileName);

                // 3. Lưu file vào đó
                using (var fileStream = new FileStream(uploadPath, FileMode.Create))
                {
                    await ImageUpload.CopyToAsync(fileStream);
                }

                // 4. Lưu đường dẫn vào Database (để hiển thị sau này)
                PortfolioItem.ImageUrl = "/uploads/" + fileName;
            }
            else
            {
                // Nếu lười không up ảnh thì dùng ảnh giữ chỗ
                PortfolioItem.ImageUrl = "https://via.placeholder.com/300x200?text=No+Image";
            }

            // Đóng dấu giờ UTC
            PortfolioItem.CreatedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

            _context.PortfolioItems.Add(PortfolioItem);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Index");
        }
    }
}