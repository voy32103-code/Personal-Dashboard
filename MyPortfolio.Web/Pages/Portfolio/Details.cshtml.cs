using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyPortfolio.Core.Entities;
using MyPortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MyPortfolio.Web.Pages.Portfolio
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public PortfolioItem PortfolioItem { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var portfolioItem = await _context.PortfolioItems.FirstOrDefaultAsync(m => m.Id == id);

            if (portfolioItem == null)
            {
                return NotFound();
            }

            PortfolioItem = portfolioItem;
            return Page();
        }
    }
}