using System.ComponentModel.DataAnnotations;

namespace MyPortfolio.Core.Entities
{
    // M-3: Cleanup unused usings (Linq, Text, Threading, Collections không cần thiết trong entity)
    public class QrScanLog
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public DateTime ScannedAt { get; set; }
        public string? IPAddress { get; set; }
    }
}