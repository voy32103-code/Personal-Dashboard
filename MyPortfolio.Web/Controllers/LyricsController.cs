using Microsoft.AspNetCore.Mvc;
using MyPortfolio.Core.Interfaces;
using MyPortfolio.Infrastructure.Data;

namespace MyPortfolio.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LyricsController : ControllerBase
    {
        private readonly ILyricsService _lyricsService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LyricsController> _logger;

        public LyricsController(ILyricsService lyricsService, ApplicationDbContext context, ILogger<LyricsController> logger)
        {
            _lyricsService = lyricsService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Tìm kiếm lời bài hát tự động từ LRCLIB theo tên bài hát và ca sĩ
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchLyrics([FromQuery] string title, [FromQuery] string? artist, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return BadRequest(new { success = false, message = "Vui lòng cung cấp tên bài hát (title)." });
            }

            var result = await _lyricsService.GetLyricsAsync(title, artist, cancellationToken);
            if (result == null || (string.IsNullOrWhiteSpace(result.SyncedLyrics) && string.IsNullOrWhiteSpace(result.PlainLyrics)))
            {
                return NotFound(new { success = false, message = "Không tìm thấy lời bài hát từ LRCLIB." });
            }

            return Ok(new
            {
                success = true,
                trackName = result.TrackName,
                artistName = result.ArtistName,
                albumName = result.AlbumName,
                duration = result.Duration,
                syncedLyrics = result.SyncedLyrics,
                plainLyrics = result.PlainLyrics
            });
        }

        /// <summary>
        /// Tự động lấy lời bài hát cho một bài đã có trong Database nếu chưa có lời
        /// </summary>
        [HttpGet("auto-fill/{id:int}")]
        public async Task<IActionResult> AutoFillLyrics(int id, CancellationToken cancellationToken)
        {
            var item = await _context.PortfolioItems.FindAsync(new object[] { id }, cancellationToken);
            if (item == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy bài hát trong cơ sở dữ liệu." });
            }

            if (!string.IsNullOrWhiteSpace(item.Lyrics))
            {
                return Ok(new
                {
                    success = true,
                    lyrics = item.Lyrics,
                    source = "database"
                });
            }

            var result = await _lyricsService.GetLyricsAsync(item.Title, item.Artist, cancellationToken);
            if (result != null && (!string.IsNullOrWhiteSpace(result.SyncedLyrics) || !string.IsNullOrWhiteSpace(result.PlainLyrics)))
            {
                var chosenLyrics = !string.IsNullOrWhiteSpace(result.SyncedLyrics) ? result.SyncedLyrics : result.PlainLyrics;
                item.Lyrics = chosenLyrics;
                await _context.SaveChangesAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    lyrics = chosenLyrics,
                    source = "lrclib_auto_saved"
                });
            }

            return NotFound(new { success = false, message = "Không tìm thấy lời bài hát từ LRCLIB." });
        }
    }
}
