namespace MyPortfolio.Core.Models
{
    public class LyricsResult
    {
        public int Id { get; set; }
        public string? TrackName { get; set; }
        public string? ArtistName { get; set; }
        public string? AlbumName { get; set; }
        public double Duration { get; set; }
        public bool Instrumental { get; set; }
        public string? PlainLyrics { get; set; }
        public string? SyncedLyrics { get; set; }
    }
}
