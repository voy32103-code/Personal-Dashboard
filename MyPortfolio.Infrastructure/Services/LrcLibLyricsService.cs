using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MyPortfolio.Core.Interfaces;
using MyPortfolio.Core.Models;

namespace MyPortfolio.Infrastructure.Services
{
    public class LrcLibLyricsService : ILyricsService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<LrcLibLyricsService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public LrcLibLyricsService(HttpClient httpClient, ILogger<LrcLibLyricsService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri("https://lrclib.net/api/");
            }

            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "MyPortfolio/2.0 (https://github.com/voy32103-code/MyPortfolio)");
            }
        }

        public async Task<LyricsResult?> GetLyricsAsync(string trackName, string? artistName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(trackName)) return null;

            try
            {
                // 1. Thử lấy chính xác theo track_name và artist_name
                var url = !string.IsNullOrWhiteSpace(artistName)
                    ? $"get?track_name={Uri.EscapeDataString(trackName.Trim())}&artist_name={Uri.EscapeDataString(artistName.Trim())}"
                    : $"get?track_name={Uri.EscapeDataString(trackName.Trim())}";

                var response = await _httpClient.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LyricsResult>(JsonOptions, cancellationToken);
                    if (result != null && (!string.IsNullOrWhiteSpace(result.SyncedLyrics) || !string.IsNullOrWhiteSpace(result.PlainLyrics)))
                    {
                        _logger.LogInformation("LRCLIB: Found exact lyrics for '{Track}' by '{Artist}'", trackName, artistName);
                        return result;
                    }
                }

                // 2. Nếu không tìm thấy chính xác, fallback sang tìm kiếm tương đối
                var searchQuery = string.IsNullOrWhiteSpace(artistName) ? trackName : $"{trackName} {artistName}";
                var searchResults = await SearchLyricsAsync(searchQuery, cancellationToken);

                // Ưu tiên bài có lời đồng bộ (syncedLyrics)
                var bestMatch = searchResults.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.SyncedLyrics))
                                ?? searchResults.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.PlainLyrics));

                return bestMatch;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LRCLIB API call failed for '{Track}' by '{Artist}'", trackName, artistName);
                return null;
            }
        }

        public async Task<List<LyricsResult>> SearchLyricsAsync(string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<LyricsResult>();

            try
            {
                var url = $"search?q={Uri.EscapeDataString(query.Trim())}";
                var response = await _httpClient.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var list = await response.Content.ReadFromJsonAsync<List<LyricsResult>>(JsonOptions, cancellationToken);
                    return list ?? new List<LyricsResult>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LRCLIB search API call failed for query '{Query}'", query);
            }

            return new List<LyricsResult>();
        }
    }
}
