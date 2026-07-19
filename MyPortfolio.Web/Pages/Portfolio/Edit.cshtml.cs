using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyPortfolio.Core.Entities;
using MyPortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using MyPortfolio.Web.Infrastructure;

namespace MyPortfolio.Web.Pages.Portfolio
{
    [Authorize(Policy = "OwnerOnly")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<EditModel> _logger;
        private readonly IFileUploadService _fileUploadService;

        public EditModel(
            ApplicationDbContext context,
            IDistributedCache cache,
            ILogger<EditModel> logger,
            IFileUploadService fileUploadService)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
            _fileUploadService = fileUploadService;
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

            string? oldImagePath = null;
            string? newImagePath = null;
            string? oldAudioPath = null;
            string? newAudioPath = null;

            // H-3: Dùng IFileUploadService — không còn copy-paste logic từ Create
            if (ImageUpload != null)
            {
                var (success, path, error) = await _fileUploadService.SaveImageAsync(ImageUpload);
                if (!success)
                {
                    ModelState.AddModelError("ImageUpload", error!);
                    PortfolioItem = existingItem;
                    return Page();
                }
                oldImagePath = existingItem.ImageUrl;
                newImagePath = path;
                existingItem.ImageUrl = path!;
            }

            // H-3: Dùng IFileUploadService cho audio
            if (AudioUpload != null)
            {
                var (success, path, error) = await _fileUploadService.SaveAudioAsync(AudioUpload);
                if (!success)
                {
                    ModelState.AddModelError("AudioUpload", error!);
                    if (newImagePath != null)
                    {
                        _fileUploadService.DeleteFile(newImagePath);
                    }
                    PortfolioItem = existingItem;
                    return Page();
                }
                oldAudioPath = existingItem.AudioUrl;
                newAudioPath = path;
                existingItem.AudioUrl = path!;
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

                // Chỉ xóa file cũ vật lý KHI DB đã lưu thành công
                if (oldImagePath != null) _fileUploadService.DeleteFile(oldImagePath);
                if (oldAudioPath != null) _fileUploadService.DeleteFile(oldAudioPath);

                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown User";
                _logger.LogInformation("PortfolioItem {ItemId} updated by {UserEmail}",
                    existingItem.Id, userEmail);

                await InvalidateRelevantCachesAsync();
            }
            catch (Exception ex)
            {
                // Nếu DB save thất bại, dọn dẹp các file mới vừa upload để tránh rác
                if (newImagePath != null) _fileUploadService.DeleteFile(newImagePath);
                if (newAudioPath != null) _fileUploadService.DeleteFile(newAudioPath);

                if (ex is DbUpdateConcurrencyException)
                {
                    _logger.LogWarning(ex, "Concurrency conflict on PortfolioItem {ItemId}", existingItem.Id);
                    ModelState.AddModelError("", "Bài hát này vừa được thay đổi bởi một người khác. Vui lòng tải lại trang.");
                }
                else
                {
                    _logger.LogError(ex, "Database update failed on PortfolioItem {ItemId}", existingItem.Id);
                    ModelState.AddModelError("", "Đã xảy ra lỗi hệ thống khi cập nhật dữ liệu. Vui lòng thử lại.");
                }

                PortfolioItem = existingItem;
                return Page();
            }

            return RedirectToPage("/Index");
        }

        // H-1: Dùng CacheKeys — fix bug cache không được invalidate
        private async Task InvalidateRelevantCachesAsync()
        {
            var keysToRemove = new[] { CacheKeys.HomeProjectsNormal, CacheKeys.HomeProjectsLibrary, CacheKeys.DashboardStats };
            foreach (var key in keysToRemove)
            {
                try { await _cache.RemoveAsync(key); }
                catch { /* Bỏ qua lỗi cache */ }
            }
        }
    }
}