using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyPortfolio.Core.Entities;
using MyPortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace MyPortfolio.Web.Pages.Portfolio
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IDistributedCache _cache;
        private readonly ILogger<EditModel> _logger;

        private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private readonly string[] _allowedAudioExtensions = { ".mp3", ".wav", ".ogg" };
        private const long MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB

        public EditModel(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            IDistributedCache cache,
            ILogger<EditModel> logger)
        {
            _context = context;
            _environment = environment;
            _cache = cache;
            _logger = logger;
        }

        [BindProperty]
        public PortfolioItem PortfolioItem { get; set; } = new PortfolioItem();

        [BindProperty]
        public IFormFile? ImageUpload { get; set; }

        [BindProperty]
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

            // 1. Lấy record gốc từ Database để so sánh và cập nhật an toàn (Tránh user fake ID)
            var existingItem = await _context.PortfolioItems.FindAsync(PortfolioItem.Id);
            if (existingItem == null) return NotFound();

            // Đảm bảo thư mục uploads tồn tại
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            // --- LOGIC UPLOAD ẢNH (EDIT) ---
            if (ImageUpload != null)
            {
                // Validate dung lượng và định dạng file
                var ext = Path.GetExtension(ImageUpload.FileName).ToLowerInvariant();
                if (!_allowedImageExtensions.Contains(ext) || ImageUpload.Length > MAX_FILE_SIZE)
                {
                    ModelState.AddModelError("ImageUpload", "File ảnh không hợp lệ hoặc vượt quá 10MB.");
                    PortfolioItem = existingItem; // Phục hồi data hiển thị
                    return Page();
                }

                // Xóa file cũ một cách an toàn
                if (IsLocalFile(existingItem.ImageUrl))
                {
                    var oldPath = Path.Combine(_environment.WebRootPath, existingItem.ImageUrl.TrimStart('/'));
                    TryDeleteOldFile(oldPath);
                }

                // Đổi tên file để chống Path Traversal
                var fileName = Guid.NewGuid().ToString("N") + ext;
                var uploadPath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(uploadPath, FileMode.CreateNew))
                {
                    await ImageUpload.CopyToAsync(fileStream);
                }
                existingItem.ImageUrl = "/uploads/" + fileName;
            }

            // --- LOGIC UPLOAD AUDIO (EDIT) ---
            if (AudioUpload != null)
            {
                var ext = Path.GetExtension(AudioUpload.FileName).ToLowerInvariant();
                if (!_allowedAudioExtensions.Contains(ext) || AudioUpload.Length > MAX_FILE_SIZE)
                {
                    ModelState.AddModelError("AudioUpload", "File audio không hợp lệ hoặc vượt quá 10MB.");
                    PortfolioItem = existingItem;
                    return Page();
                }

                if (IsLocalFile(existingItem.AudioUrl))
                {
                    var oldPath = Path.Combine(_environment.WebRootPath, existingItem.AudioUrl.TrimStart('/'));
                    TryDeleteOldFile(oldPath);
                }

                var audioName = Guid.NewGuid().ToString("N") + ext;
                var audioPath = Path.Combine(uploadsFolder, audioName);

                using (var stream = new FileStream(audioPath, FileMode.CreateNew))
                {
                    await AudioUpload.CopyToAsync(stream);
                }
                existingItem.AudioUrl = "/uploads/" + audioName;
            }

            // 2. Chỉ cập nhật các trường được phép (Tránh đè CreatedDate hoặc PlayCount)
            existingItem.Title = PortfolioItem.Title;
            existingItem.Description = PortfolioItem.Description;
            existingItem.Artist = PortfolioItem.Artist;
            existingItem.ProjectUrl = PortfolioItem.ProjectUrl;
            existingItem.Lyrics = PortfolioItem.Lyrics;

            try
            {
                await _context.SaveChangesAsync();

                // Lưu log hành động Edit
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown User";
                _logger.LogInformation($"📝 Bài hát ID={existingItem.Id} vừa được cập nhật bởi {userEmail} lúc {DateTime.UtcNow}");

                // Đập vỡ các Cache liên quan
                await InvalidateRelevantCachesAsync();
            }
            // Bắt lỗi Race Condition
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, $"Xung đột dữ liệu khi cập nhật bài hát ID {existingItem.Id}");
                ModelState.AddModelError("", "Bài hát này vừa được thay đổi bởi một người khác. Vui lòng tải lại trang.");
                PortfolioItem = existingItem;
                return Page();
            }

            return RedirectToPage("/Index");
        }
        // ==========================================
        // HELPER METHODS
        // ==========================================
        private bool IsLocalFile(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (url.Contains("no-image.png", StringComparison.OrdinalIgnoreCase)) return false;

            return url.StartsWith("/") &&
                   !url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                   !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        private void TryDeleteOldFile(string filePath)
        {
            try
            {
                var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads");
                var fullPath = Path.GetFullPath(filePath);
                var fullUploadsDir = Path.GetFullPath(uploadsDir);

                if (fullPath.StartsWith(fullUploadsDir, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Không thể xóa file cũ ({filePath}): {ex.Message}");
            }
        }

        private async Task InvalidateRelevantCachesAsync()
        {
            var keysToRemove = new[] { "home_projects_normal_none", "home_projects_library_none", "dashboard_stats" };
            foreach (var key in keysToRemove)
            {
                try { await _cache.RemoveAsync(key); }
                catch { /* Bỏ qua lỗi cache */ }
            }
        }
    }
}