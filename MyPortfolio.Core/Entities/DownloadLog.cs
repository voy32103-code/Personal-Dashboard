using System.ComponentModel.DataAnnotations;

namespace MyPortfolio.Core.Entities
{
    // M-2: Xóa duplicate using, M-3: Cleanup unused usings (Linq, Text, Threading, Collections không cần thiết)
    public class DownloadLog
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public DateTime DownloadedAt { get; set; }

        public string? IPAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}