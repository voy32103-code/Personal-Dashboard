using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyPortfolio.Core.Entities;
using MyPortfolio.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;

namespace MyPortfolio.Web.Pages.Portfolio
{
    [Authorize]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<DeleteModel> _logger; 
        private readonly IDistributedCache _cache;     

        public DeleteModel(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<DeleteModel> logger,
            IDistributedCache cache)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
            _cache = cache;
        }

        [BindProperty]
        public PortfolioItem PortfolioItem { get; set; } = new PortfolioItem();

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
                var deletedFilesSuccessfully = true;

                // Ưu tiên xóa File trước, xóa Database sau
                //  Kiểm tra xem URL có phải là file trên server nội bộ không
                if (IsLocalFile(item.ImageUrl))
                {
                    var imagePath = Path.Combine(_environment.WebRootPath, item.ImageUrl.TrimStart('/'));
                    if (!TryDeleteFile(imagePath)) deletedFilesSuccessfully = false;
                }

                if (IsLocalFile(item.AudioUrl))
                {
                    var audioPath = Path.Combine(_environment.WebRootPath, item.AudioUrl.TrimStart('/'));
                    if (!TryDeleteFile(audioPath)) deletedFilesSuccessfully = false;
                }

                // Nếu có lỗi khi xóa file (ví dụ file bị lock), trả về thông báo lỗi, không xóa DB
                if (!deletedFilesSuccessfully)
                {
                    ModelState.AddModelError("", "Hệ thống đang bận, không thể xóa file vật lý lúc này. Vui lòng thử lại sau.");
                    PortfolioItem = item;
                    return Page();
                }

                // Nếu file đã xóa sạch (hoặc không có file), tiến hành xóa Database
                _context.PortfolioItems.Remove(item);
                await _context.SaveChangesAsync();

                //  Lưu lịch sử thao tác xóa (Audit Trail)
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown User";
                _logger.LogWarning($"⚠️ Đã xóa bài hát: ID={item.Id}, Title='{item.Title}', DeletedBy='{userEmail}'");

                //  Đập vỡ các Cache liên quan để dữ liệu trên Trang chủ biến mất ngay lập tức
                await InvalidateRelevantCachesAsync();
            }

            return RedirectToPage("/Index");
        }

        // ==========================================
        // HELPER METHODS
        // ==========================================

        // Kiểm tra xem link ảnh/nhạc là link local hay external (VD: http://...)
        // Chỉ được phép xóa file Local (bắt đầu bằng dấu / và không chứa chữ placeholder)
        private bool IsLocalFile(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;

            // Không xóa ảnh no-image mặc định của hệ thống
            if (url.Contains("no-image.png", StringComparison.OrdinalIgnoreCase)) return false;

            return url.StartsWith("/") &&
                   !url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                   !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }
        //  Xóa file với cơ chế bắt lỗi an toàn và chống Path Traversal
        private bool TryDeleteFile(string filePath)
        {
            try
            {
                //  Ngăn chặn Path Traversal (Tuyệt đối không cho phép truy cập ra ngoài thư mục uploads)
                var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads");
                var fullPath = Path.GetFullPath(filePath);
                var fullUploadsDir = Path.GetFullPath(uploadsDir);

                if (!fullPath.StartsWith(fullUploadsDir, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError($"❌ Cảnh báo bảo mật: Phát hiện nỗ lực xóa file ngoài thư mục cho phép: {filePath}");
                    return false;
                }

                //  Xóa file an toàn với Try-Catch
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                    return true;
                }

                // Trả về true nếu file đã bị ai đó xóa từ trước (để tiến trình xóa DB vẫn tiếp tục)
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Lỗi hệ thống khi cố gắng xóa file vật lý: {filePath}");
                return false;
            }
        }
        // Xóa các Redis Cache bị ảnh hưởng sau khi xóa bài hát
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
                try
                {
                    await _cache.RemoveAsync(key);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Không thể xóa cache {key}: {ex.Message}");
                }
            }
        }
    }
}