using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace MyPortfolio.Core.Entities
{
    // M-3: Cleanup unused usings (Linq, Text, Threading không cần thiết trong entity)
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Summary { get; set; }
        public string? AvatarPath { get; set; }

        public int CvDownloadCount { get; set; }
        public int QrScanCount { get; set; }

        public ICollection<DownloadLog> DownloadLogs { get; set; } = new List<DownloadLog>();
        public ICollection<QrScanLog> QrScanLogs { get; set; } = new List<QrScanLog>();
    }
}