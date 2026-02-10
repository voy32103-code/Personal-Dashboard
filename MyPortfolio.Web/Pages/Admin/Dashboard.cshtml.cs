using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyPortfolio.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;

namespace MyPortfolio.Web.Pages.Admin
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- SỬA LỖI Ở ĐÂY: Khởi tạo mảng rỗng ngay lập tức ---
        public string[] SongTitles { get; set; } = Array.Empty<string>();
        public int[] PlayCounts { get; set; } = Array.Empty<int>();

        public int TotalSongs { get; set; }
        public int TotalPlays { get; set; }

        public async Task OnGetAsync()
        {
            var songs = await _context.PortfolioItems
                .Select(p => new { p.Title, p.PlayCount })
                .ToListAsync();

            // Gán dữ liệu (Nếu không có bài hát nào, nó vẫn là mảng rỗng chứ không null)
            SongTitles = songs.Select(s => s.Title ?? "Untitled").ToArray();
            PlayCounts = songs.Select(s => s.PlayCount).ToArray();

            TotalSongs = songs.Count;
            TotalPlays = songs.Sum(s => s.PlayCount);
        }
    }
}