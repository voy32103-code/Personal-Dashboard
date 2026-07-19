using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyPortfolio.Core.Entities;

namespace MyPortfolio.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<PortfolioItem> PortfolioItems { get; set; }
        public new DbSet<User> Users { get; set; }
        public DbSet<DownloadLog> DownloadLogs { get; set; }
        public DbSet<QrScanLog> QrScanLogs { get; set; }

        // L-2: Thêm OnModelCreating với indexes để PostgreSQL không bị full table scan
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Index cho các trường thường xuyên dùng trong ORDER BY / WHERE
            builder.Entity<PortfolioItem>()
                .HasIndex(p => p.CreatedDate)
                .HasDatabaseName("IX_PortfolioItems_CreatedDate");

            builder.Entity<PortfolioItem>()
                .HasIndex(p => p.PlayCount)
                .HasDatabaseName("IX_PortfolioItems_PlayCount");

            builder.Entity<DownloadLog>()
                .HasIndex(d => d.DownloadedAt)
                .HasDatabaseName("IX_DownloadLogs_DownloadedAt");

            builder.Entity<QrScanLog>()
                .HasIndex(s => s.ScannedAt)
                .HasDatabaseName("IX_QrScanLogs_ScannedAt");

            // Column constraints
            builder.Entity<PortfolioItem>()
                .Property(p => p.Title).HasMaxLength(200).IsRequired();

            builder.Entity<PortfolioItem>()
                .Property(p => p.Description).HasMaxLength(2000);

            builder.Entity<User>()
                .Property(u => u.Name).HasMaxLength(200).IsRequired();
        }
    }
}