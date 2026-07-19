using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyPortfolio.Core.Entities;
using MyPortfolio.Infrastructure.Data;
using Xunit;

namespace MyPortfolio.Tests
{
    public class DatabaseTests
    {
        private DbContextOptions<ApplicationDbContext> CreateNewContextOptions()
        {
            // Sử dụng tên cơ sở dữ liệu duy nhất cho mỗi bài kiểm thử để chạy song song an toàn
            return new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task Can_Insert_And_Retrieve_PortfolioItem()
        {
            // Arrange
            var options = CreateNewContextOptions();
            var portfolioItem = new PortfolioItem
            {
                Title = "Dự án mẫu",
                Description = "Mô tả dự án mẫu",
                ImageUrl = "/uploads/img.png",
                ProjectUrl = "https://github.com",
                Artist = "Nghệ sĩ",
                Lyrics = "[00:01.00]Lời bài hát mẫu"
            };

            // Act: Insert
            using (var context = new ApplicationDbContext(options))
            {
                context.PortfolioItems.Add(portfolioItem);
                await context.SaveChangesAsync();
            }

            // Assert: Retrieve
            using (var context = new ApplicationDbContext(options))
            {
                var retrieved = await context.PortfolioItems.FirstOrDefaultAsync();
                Assert.NotNull(retrieved);
                Assert.Equal("Dự án mẫu", retrieved.Title);
                Assert.Equal("Mô tả dự án mẫu", retrieved.Description);
                Assert.Equal("/uploads/img.png", retrieved.ImageUrl);
                Assert.Equal("https://github.com", retrieved.ProjectUrl);
                Assert.Equal("Nghệ sĩ", retrieved.Artist);
                Assert.Equal("[00:01.00]Lời bài hát mẫu", retrieved.Lyrics);
                Assert.False(retrieved.IsFavorite);
                Assert.Equal(0, retrieved.PlayCount);
            }
        }

        [Fact]
        public async Task Can_Update_PortfolioItem()
        {
            // Arrange
            var options = CreateNewContextOptions();
            var portfolioItem = new PortfolioItem { Title = "Tiêu đề cũ", Description = "Mô tả cũ" };

            using (var context = new ApplicationDbContext(options))
            {
                context.PortfolioItems.Add(portfolioItem);
                await context.SaveChangesAsync();
            }

            // Act: Update
            using (var context = new ApplicationDbContext(options))
            {
                var retrieved = await context.PortfolioItems.FirstAsync();
                retrieved.Title = "Tiêu đề mới";
                retrieved.Description = "Mô tả mới";
                retrieved.IsFavorite = true;
                retrieved.PlayCount = 42;
                await context.SaveChangesAsync();
            }

            // Assert: Verify update
            using (var context = new ApplicationDbContext(options))
            {
                var retrieved = await context.PortfolioItems.FirstAsync();
                Assert.Equal("Tiêu đề mới", retrieved.Title);
                Assert.Equal("Mô tả mới", retrieved.Description);
                Assert.True(retrieved.IsFavorite);
                Assert.Equal(42, retrieved.PlayCount);
            }
        }

        [Fact]
        public async Task Can_Delete_PortfolioItem()
        {
            // Arrange
            var options = CreateNewContextOptions();
            var portfolioItem = new PortfolioItem { Title = "Xóa tôi", Description = "Mô tả xóa" };

            using (var context = new ApplicationDbContext(options))
            {
                context.PortfolioItems.Add(portfolioItem);
                await context.SaveChangesAsync();
            }

            // Act: Delete
            using (var context = new ApplicationDbContext(options))
            {
                var retrieved = await context.PortfolioItems.FirstAsync();
                context.PortfolioItems.Remove(retrieved);
                await context.SaveChangesAsync();
            }

            // Assert: Verify deleted
            using (var context = new ApplicationDbContext(options))
            {
                var count = await context.PortfolioItems.CountAsync();
                Assert.Equal(0, count);
            }
        }
    }
}
