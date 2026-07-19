using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyPortfolio.Core.Entities;
using MyPortfolio.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using MyPortfolio.Web.Infrastructure;

namespace MyPortfolio.Web.Pages.Portfolio
{
    [Authorize(Policy = "OwnerOnly")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<CreateModel> _logger;
        private readonly IFileUploadService _fileUploadService; // H-3: DI thay vì duplicate logic

        public CreateModel(
            ApplicationDbContext context,
            IDistributedCache cache,
            ILogger<CreateModel> logger,
            IFileUploadService fileUploadService)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
            _fileUploadService = fileUploadService;
        }

        [BindProperty]
        public PortfolioItem PortfolioItem { get; set; } = new PortfolioItem(); // Tránh null 

        [BindProperty]
        public IFormFile? ImageUpload { get; set; }

        [BindProperty]
        public IFormFile? AudioUpload { get; set; }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return Page();

            // Danh sách file đã upload để dọn dẹp (Rollback) nếu DB lưu thất bại
            var uploadedPaths = new List<string>();

            try
            {
                // --- H-3: Dùng IFileUploadService thay vì logic được copy-paste ---
                if (ImageUpload != null)
                {
                    var (success, path, error) = await _fileUploadService.SaveImageAsync(ImageUpload, cancellationToken);
                    if (!success)
                    {
                        ModelState.AddModelError("ImageUpload", error!);
                        return Page();
                    }
                    PortfolioItem.ImageUrl = path!;
                    uploadedPaths.Add(path!);
                }
                else
                {
                    PortfolioItem.ImageUrl = "/images/no-image.png";
                }

                if (AudioUpload != null)
                {
                    var (success, path, error) = await _fileUploadService.SaveAudioAsync(AudioUpload, cancellationToken);
                    if (!success)
                    {
                        ModelState.AddModelError("AudioUpload", error!);
                        _fileUploadService.RollbackFiles(uploadedPaths);
                        return Page();
                    }
                    PortfolioItem.AudioUrl = path!;
                    uploadedPaths.Add(path!);
                }
                else
                {
                    PortfolioItem.AudioUrl = string.Empty;
                }

                PortfolioItem.CreatedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                _context.PortfolioItems.Add(PortfolioItem);
                await _context.SaveChangesAsync(cancellationToken);

                // H-1: Dùng CacheKeys constants
                await InvalidateRelevantCachesAsync();
                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save portfolio item. Rolling back uploaded files.");
                _fileUploadService.RollbackFiles(uploadedPaths);
                ModelState.AddModelError("", "Đã xảy ra lỗi hệ thống khi lưu dữ liệu. Vui lòng thử lại.");
                return Page();
            }
        }


        // H-1: Dùng CacheKeys constants — fix bug invalidation không hoạt động
        private async Task InvalidateRelevantCachesAsync()
        {
            var keysToRemove = new[]
            {
                CacheKeys.HomeProjectsNormal,
                CacheKeys.HomeProjectsLibrary,
                CacheKeys.DashboardStats
            };

            foreach (var key in keysToRemove)
            {
                await _cache.RemoveAsync(key);
            }
        }
    }
}