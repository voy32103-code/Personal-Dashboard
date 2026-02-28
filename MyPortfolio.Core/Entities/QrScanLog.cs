using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace MyPortfolio.Core.Entities
{
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