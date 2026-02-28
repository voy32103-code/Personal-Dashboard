using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace MyPortfolio.Core.Entities
{
    public class DownloadLog
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public DateTime DownloadedAt { get; set; }

        // Lưu IP và Trình duyệt để phân tích
        public string? IPAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}