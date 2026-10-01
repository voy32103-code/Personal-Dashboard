using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using MyPortfolio.Core.Entities;
using MyPortfolio.Core.Interfaces;
using MyPortfolio.Core.Models;
using MyPortfolio.Infrastructure.Data;
using MyPortfolio.Infrastructure.Services;
using MyPortfolio.Web.Controllers;

namespace MyPortfolio.Tests
{
    public class LyricsServiceTests
    {
        [Fact]
        public async Task LrcLibLyricsService_ReturnsSyncedLyrics_WhenApiSucceeds()
        {
            // Arrange
            var responsePayload = new
            {
                id = 123,
                name = "Test Song",
                trackName = "Test Song",
                artistName = "Test Artist",
                syncedLyrics = "[00:10.50]Hello world\n[00:15.00]Second line",
                plainLyrics = "Hello world\nSecond line",
                instrumental = false
            };

            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(responsePayload))
                });

            var httpClient = new HttpClient(mockHandler.Object)
            {
                BaseAddress = new Uri("https://lrclib.net/api/")
            };

            var loggerMock = new Mock<ILogger<LrcLibLyricsService>>();
            var service = new LrcLibLyricsService(httpClient, loggerMock.Object);

            // Act
            var result = await service.GetLyricsAsync("Test Song", "Test Artist");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Song", result.TrackName);
            Assert.Equal("Test Artist", result.ArtistName);
            Assert.Contains("[00:10.50]Hello world", result.SyncedLyrics);
        }

        [Fact]
        public async Task LyricsController_AutoFill_UpdatesDatabase_WhenLyricsFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var item = new PortfolioItem
            {
                Id = 1,
                Title = "Bài Ca Hy Vọng",
                Artist = "Ca Sĩ A",
                Lyrics = null
            };
            context.PortfolioItems.Add(item);
            await context.SaveChangesAsync();

            var lyricsServiceMock = new Mock<ILyricsService>();
            lyricsServiceMock
                .Setup(s => s.GetLyricsAsync("Bài Ca Hy Vọng", "Ca Sĩ A", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LyricsResult
                {
                    TrackName = "Bài Ca Hy Vọng",
                    ArtistName = "Ca Sĩ A",
                    SyncedLyrics = "[00:05.00]Từng đàn chim bay"
                });

            var loggerMock = new Mock<ILogger<LyricsController>>();
            var controller = new LyricsController(lyricsServiceMock.Object, context, loggerMock.Object);

            // Act
            var result = await controller.AutoFillLyrics(1, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedItem = await context.PortfolioItems.FindAsync(1);
            Assert.NotNull(updatedItem);
            Assert.Equal("[00:05.00]Từng đàn chim bay", updatedItem.Lyrics);
        }
    }
}
