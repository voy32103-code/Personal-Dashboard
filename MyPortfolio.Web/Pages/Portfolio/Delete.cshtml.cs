using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyPortfolio.Core.Entities;
using MyPortfolio.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;
using MyPortfolio.Web.Infrastructure;

namespace MyPortfolio.Web.Pages.Portfolio
{
    [Authorize(Policy = "OwnerOnly")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeleteModel> _logger; 
        private readonly IDistributedCache _cache;     
        private readonly IFileUploadService _fileUploadService;

        public DeleteModel(
            ApplicationDbContext context,
            ILogger<DeleteModel> logger,
            IDistributedCache cache,
            IFileUploadService fileUploadService)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _fileUploadService = fileUploadService;
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
                var imageUrl = item.ImageUrl;
                var audioUrl = item.AudioUrl;

                try
                {
                    _context.PortfolioItems.Remove(item);
                    await _context.SaveChangesAsync();

                    // Xóa file vật lý chỉ khi CSDL đã được cập nhật thành công
                    _fileUploadService.DeleteFile(imageUrl);
                    _fileUploadService.DeleteFile(audioUrl);

                    var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown User";
                    _logger.LogWarning("PortfolioItem {ItemId} '{Title}' deleted by {UserEmail}",
                        item.Id, item.Title, userEmail);

                    await InvalidateRelevantCachesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete PortfolioItem {ItemId}", item.Id);
                    ModelState.AddModelError("", "Đã xảy ra lỗi hệ thống khi xóa bài viết. Vui lòng thử lại.");
                    PortfolioItem = item;
                    return Page();
                }
            }

            return RedirectToPage("/Index");
        }
        // H-1: CacheKeys constants — fix bug invalidation với key format sai
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
                try
                {
                    await _cache.RemoveAsync(key);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to remove cache key: {Key}", key);
                }
            }
        }
    }
}