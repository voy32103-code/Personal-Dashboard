using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore; // Nhớ có dòng này để dùng ToListAsync
using MyPortfolio.Core.Entities;
using MyPortfolio.Infrastructure.Data;

namespace MyPortfolio.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        // Kết nối Database
        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Biến này sẽ chứa danh sách các dự án lấy được
        public IList<PortfolioItem> Projects { get; set; } = new List<PortfolioItem>();

        public async Task OnGetAsync()
        {
            // Lệnh truy vấn lấy tất cả dự án từ bảng PortfolioItems
            Projects = await _context.PortfolioItems.ToListAsync();
        }
    }
}