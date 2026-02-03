using Microsoft.AspNetCore.Identity.EntityFrameworkCore; // Thêm dòng này
using Microsoft.EntityFrameworkCore;
using MyPortfolio.Core.Entities;

namespace MyPortfolio.Infrastructure.Data
{
    // ĐỔI: Inherit từ IdentityDbContext thay vì DbContext
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<PortfolioItem> PortfolioItems { get; set; }
    }
}