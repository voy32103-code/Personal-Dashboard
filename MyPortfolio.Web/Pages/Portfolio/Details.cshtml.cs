using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyPortfolio.Core.Entities;
using MyPortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace MyPortfolio.Web.Pages.Portfolio
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<DetailsModel> _logger;

        // Bổ sung Cache và Logger vào Constructor
        public DetailsModel(
            ApplicationDbContext context,
            IDistributedCache cache,
            ILogger<DetailsModel> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public PortfolioItem PortfolioItem { get; set; } = default!;

        // 1. Lấy thông tin chi tiết bài hát (PUBLIC - Ai cũng xem được)
        //  Bỏ [Authorize] ở Class level, để hàm GET này public
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            //  Thêm AsNoTracking() để tối ưu hiệu năng cho trang Read-Only
            // Nếu có bảng phụ (Artist, Category), bạn có thể chain thêm .Include(p => p.Artist) vào đây
            var portfolioItem = await _context.PortfolioItems
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (portfolioItem == null) return NotFound();

            PortfolioItem = portfolioItem;
            return Page();
        }

        // 2. Xử lý khi bấm nút Thả Tim (PRIVATE - Phải đăng nhập)
        //  Gắn [Authorize] chỉ định cho riêng hàm này
        [Authorize]
        public async Task<IActionResult> OnPostToggleHeartAsync(int id)
        {
            try
            {
                var item = await _context.PortfolioItems.FindAsync(id);

                //Xử lý an toàn khi không tìm thấy Item
                if (item == null)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return new JsonResult(new { success = false, error = "Không tìm thấy bài hát." });

                    return NotFound();
                }

                // Đảo ngược trạng thái Thích <-> Không thích
                item.IsFavorite = !item.IsFavorite;
                await _context.SaveChangesAsync();

                // Xóa Cache ngay lập tức để Homepage/Library nhận dữ liệu mới
                await InvalidateRelevantCachesAsync();

                // Trả về JSON nếu gọi bằng AJAX (trải nghiệm mượt, không reload trang)
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return new JsonResult(new { success = true, isFavorite = item.IsFavorite });
                }

                return RedirectToPage(new { id = id });
            }
            // Xử lý Race Condition (2 người cùng thao tác lúc)
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning($"Xung đột dữ liệu khi thả tim bài hát ID {id}: {ex.Message}");

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return new JsonResult(new { success = false, error = "Hệ thống đang bận, vui lòng thử lại." });

                return RedirectToPage(new { id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi không xác định khi thả tim bài hát ID {id}");
                return RedirectToPage(new { id = id });
            }
        }

        // ==========================================
        // HELPER METHODS
        // ==========================================
        // BUG D2 FIX: Dọn dẹp Cache liên quan  
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