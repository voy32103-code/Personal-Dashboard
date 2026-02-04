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
        private readonly IWebHostEnvironment _environment;

        public CreateModel(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public PortfolioItem PortfolioItem { get; set; } = default!;

        [BindProperty]
        public IFormFile? ImageUpload { get; set; }

        [BindProperty] // ← ĐÃ THÊM ATTRIBUTE NÀY
        public IFormFile? AudioUpload { get; set; }

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
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageUpload.FileName);
                var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", fileName);

                using (var fileStream = new FileStream(uploadPath, FileMode.Create))
                {
                    await ImageUpload.CopyToAsync(fileStream);
                }

                PortfolioItem.ImageUrl = "/uploads/" + fileName;
            }
            else
            {
                PortfolioItem.ImageUrl = "https://via.placeholder.com/300x200?text=No+Image";
            }

            // --- LOGIC UPLOAD AUDIO ---
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

            PortfolioItem.CreatedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

            _context.PortfolioItems.Add(PortfolioItem);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Index");
        }
    }
}