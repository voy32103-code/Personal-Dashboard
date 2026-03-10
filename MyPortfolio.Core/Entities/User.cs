using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace MyPortfolio.Core.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        // Thông tin để in lên CV
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Summary { get; set; }
        public string? AvatarPath { get; set; } // THÊM DÒNG NÀY

        // Số liệu thống kê sơ bộ
        public int CvDownloadCount { get; set; }
        public int QrScanCount { get; set; }

        // Liên kết (1 User có nhiều lượt tải/quét)
        public ICollection<DownloadLog> DownloadLogs { get; set; } = new List<DownloadLog>();
        public ICollection<QrScanLog> QrScanLogs { get; set; } = new List<QrScanLog>();
    }
}