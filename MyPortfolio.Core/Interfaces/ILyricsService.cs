using MyPortfolio.Core.Models;

namespace MyPortfolio.Core.Interfaces
{
    public interface ILyricsService
    {
        Task<LyricsResult?> GetLyricsAsync(string trackName, string? artistName, CancellationToken cancellationToken = default);
        Task<List<LyricsResult>> SearchLyricsAsync(string query, CancellationToken cancellationToken = default);
    }
}
