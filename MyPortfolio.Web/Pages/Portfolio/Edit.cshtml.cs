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
        private readonly IWebHostEnvironment _environment;

        public EditModel(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public PortfolioItem PortfolioItem { get; set; } = default!;

        [BindProperty]
        public IFormFile? ImageUpload { get; set; }

        [BindProperty] // ← THÊM ATTRIBUTE NÀY
        public IFormFile? AudioUpload { get; set; }

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
                // Xóa ảnh cũ nếu tồn tại
                if (!string.IsNullOrEmpty(PortfolioItem.ImageUrl) &&
                    !PortfolioItem.ImageUrl.Contains("placeholder"))
                {
                    var oldImagePath = Path.Combine(_environment.WebRootPath, PortfolioItem.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Upload ảnh mới
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageUpload.FileName);
                var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", fileName);

                using (var fileStream = new FileStream(uploadPath, FileMode.Create))
                {
                    await ImageUpload.CopyToAsync(fileStream);
                }

                PortfolioItem.ImageUrl = "/uploads/" + fileName;
            }

            // --- LOGIC UPLOAD AUDIO (EDIT) ---
            if (AudioUpload != null)
            {
                var audioName = Guid.NewGuid().ToString() + Path.GetExtension(AudioUpload.FileName);
                var audioPath = Path.Combine(_environment.WebRootPath, "uploads", audioName);

                using (var stream = new FileStream(audioPath, FileMode.Create))
                {
                    await AudioUpload.CopyToAsync(stream);
                }

                PortfolioItem.AudioUrl = "/uploads/" + audioName;
            }

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