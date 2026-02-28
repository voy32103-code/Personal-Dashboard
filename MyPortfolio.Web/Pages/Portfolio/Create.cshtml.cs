using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyPortfolio.Core.Entities;
using MyPortfolio.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace MyPortfolio.Web.Pages.Portfolio
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IDistributedCache _cache;
        private readonly ILogger<CreateModel> _logger; // Dùng để log lỗi 

        // Danh sách đuôi file được phép upload 
        private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private readonly string[] _allowedAudioExtensions = { ".mp3", ".wav", ".ogg" };

        public CreateModel(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            IDistributedCache cache,
            ILogger<CreateModel> logger)
        {
            _context = context;
            _environment = environment;
            _cache = cache;
            _logger = logger;
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

        // Bổ sung CancellationToken để xử lý Timeout 
        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            //  Đảm bảo model hợp lệ trước khi xử lý
            if (!ModelState.IsValid) return Page();

            // Đảm bảo thư mục uploads tồn tại, nếu chưa có thì tạo mới
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Danh sách file đã upload để dọn dẹp (Rollback) nếu DB lưu thất bại 
            var uploadedFilePaths = new List<string>();

            try
            {
                // --- LOGIC UPLOAD ẢNH ---
                if (ImageUpload != null)
                {
                    var ext = Path.GetExtension(ImageUpload.FileName).ToLowerInvariant();

                    //  Validate file extension
                    if (!_allowedImageExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("ImageUpload", "Chỉ chấp nhận file ảnh (.jpg, .png, .gif, .webp).");
                        return Page();
                    }

                    // Chống Directory Traversal bằng cách tự sinh tên mới với Guid("N") (không có dấu gạch ngang)
                    var fileName = Guid.NewGuid().ToString("N") + ext;
                    var uploadPath = Path.Combine(uploadsFolder, fileName);

                    // Dùng FileMode.CreateNew để tránh ghi đè file hiện có (Race Condition)
                    using (var fileStream = new FileStream(uploadPath, FileMode.CreateNew))
                    {
                        await ImageUpload.CopyToAsync(fileStream, cancellationToken);
                    }

                    PortfolioItem.ImageUrl = "/uploads/" + fileName;
                    uploadedFilePaths.Add(uploadPath); // Ghi nhận đã upload
                }
                else
                {
                    // Dùng ảnh local thay vì external link dễ bị die
                    PortfolioItem.ImageUrl = "/images/no-image.png";
                }

                // --- LOGIC UPLOAD AUDIO ---
                if (AudioUpload != null)
                {
                    var ext = Path.GetExtension(AudioUpload.FileName).ToLowerInvariant();

                    if (!_allowedAudioExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("AudioUpload", "Chỉ chấp nhận file âm thanh (.mp3, .wav, .ogg).");
                        RollbackUploadedFiles(uploadedFilePaths); // Xóa ảnh đã up (nếu có) trước khi return
                        return Page();
                    }

                    var audioName = Guid.NewGuid().ToString("N") + ext;
                    var audioPath = Path.Combine(uploadsFolder, audioName);

                    using (var stream = new FileStream(audioPath, FileMode.CreateNew))
                    {
                        await AudioUpload.CopyToAsync(stream, cancellationToken);
                    }

                    PortfolioItem.AudioUrl = "/uploads/" + audioName;
                    uploadedFilePaths.Add(audioPath); // Ghi nhận đã upload
                }
                else
                {
                    // Cấp giá trị mặc định an toàn nếu không có audio
                    PortfolioItem.AudioUrl = "";
                }

                // Xử lý lưu Database
                PortfolioItem.CreatedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                _context.PortfolioItems.Add(PortfolioItem);

                // Truyền cancellationToken vào DB Save
                await _context.SaveChangesAsync(cancellationToken);

                // Gọi hàm Helper xóa tất cả Cache liên quan để dữ liệu mới được hiển thị ngay
                await InvalidateRelevantCachesAsync();

                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                //  Bắt lỗi hệ thống (Database chết, mất mạng...) và Dọn dẹp file rác
                _logger.LogError(ex, "Lỗi khi lưu bài hát. Hệ thống đang rollback các file đã upload...");
                RollbackUploadedFiles(uploadedFilePaths);

                ModelState.AddModelError("", "Đã xảy ra lỗi hệ thống khi lưu dữ liệu. Vui lòng thử lại.");
                return Page();
            }
        }

        // ==========================================
        // HELPER METHODS
        // ==========================================

        /// <summary>
        /// BUG 7: Dọn dẹp file rác (Orphaned files) nếu quá trình lưu DB thất bại
        /// </summary>
        private void RollbackUploadedFiles(List<string> filePaths)
        {
            foreach (var path in filePaths)
            {
                if (System.IO.File.Exists(path))
                {
                    try { System.IO.File.Delete(path); }
                    catch (Exception ex) { _logger.LogWarning(ex, $"Không thể xóa file rác: {path}"); }
                }
            }
        }

        /// <summary>
        /// BUG 9: Xóa triệt để các Cache liên quan sau khi tạo mới
        /// </summary>
        private async Task InvalidateRelevantCachesAsync()
        {
            var keysToRemove = new[]
            {
                "home_projects_normal_none",
                "home_projects_library_none",
                "dashboard_stats"
            };

            foreach (var key in keysToRemove)
            {
                await _cache.RemoveAsync(key);
            }
        }
    }
}