using Microsoft.EntityFrameworkCore;
using MyPortfolio.Core.Entities;

namespace MyPortfolio.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Khai báo bảng PortfolioItems
        public DbSet<PortfolioItem> PortfolioItems { get; set; }
    }
}