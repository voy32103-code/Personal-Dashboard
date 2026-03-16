using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyPortfolio.Core.Entities;
using MyPortfolio.Infrastructure.Data;
using Microsoft.Extensions.Caching.Distributed;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;

namespace MyPortfolio.Web.Pages
{
    public class SkillItem
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Badge { get; set; } = string.Empty;
        public string BadgeColor { get; set; } = string.Empty;
        public int Progress { get; set; }
    }

    public class ProfileModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;

        public ProfileModel(ApplicationDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public User ProfileUser { get; set; } = default!;
        public int LinesOfCode { get; set; } = 2700;
        public string GithubUrl { get; set; } = "https://github.com/vohungyen";
        public List<SkillItem> Skills { get; set; } = new();

        public string FullName => ProfileUser?.Name ?? "VÕ HƯNG YÊN";
        public string Title => ProfileUser?.Summary ?? "SE Student @ HUFLIT • .NET Full-Stack Developer";
        public int UserId => ProfileUser?.Id ?? 1;

        // ================================================================
        // 1. LOAD TRANG PROFILE
        // ================================================================
        public async Task<IActionResult> OnGetAsync(int id = 1)
        {
            if (id != 1) return NotFound();

            var user = await _context.Users.FindAsync(id);
            var cachedSkills = await _cache.GetStringAsync("skills");

            if (cachedSkills != null)
            {
                Skills = JsonSerializer.Deserialize<List<SkillItem>>(cachedSkills) ?? BuildDefaultSkills();
            }
            else
            {
                Skills = BuildDefaultSkills();
                var cacheOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) };
                await _cache.SetStringAsync("skills", JsonSerializer.Serialize(Skills), cacheOptions);
            }

            if (user == null)
            {
                user = new User
                {
                    Name = "VÕ HƯNG YÊN",
                    Email = "voy32103@gmail.com",
                    Phone = "0355161941",
                    Summary = "SE Student @ HUFLIT • .NET Full-Stack Developer",
                    AvatarPath = "/uploads/e7d2d820-bceb-4d10-8c80-afb8d5a88220.jpg",
                    CvDownloadCount = 0,
                    QrScanCount = 0
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            ProfileUser = user;
            return Page();
        }

        private static List<SkillItem> BuildDefaultSkills() => new()
        {
            new SkillItem { Name = ".NET 8 & System Architecture", Description = "Razor Pages, SignalR", Icon = "fab fa-microsoft", Color = "text-primary", Badge = "Core", BadgeColor = "bg-primary", Progress = 95 },
            new SkillItem { Name = "Database & EF Core", Description = "Neon PostgreSQL & LINQ", Icon = "fas fa-database", Color = "text-info", Badge = "Data", BadgeColor = "bg-info", Progress = 90 },
            new SkillItem { Name = "Redis Distributed Caching", Description = "Cache-Aside, Performance", Icon = "fas fa-bolt", Color = "text-warning", Badge = "Speed", BadgeColor = "bg-warning text-dark", Progress = 85 },
            new SkillItem { Name = "Security & OAuth 2.0", Description = "Google Auth, Anti-Traversal", Icon = "fas fa-shield-alt", Color = "text-danger", Badge = "Sec", BadgeColor = "bg-danger", Progress = 88 }
        };

        // ================================================================
        // 2. DOWNLOAD CV TĨNH 
        // ================================================================
        public async Task<IActionResult> OnPostDownloadCvAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var agent = Request.Headers["User-Agent"].ToString();

            // Ghi Log vào DB
            _context.DownloadLogs.Add(new DownloadLog
            {
                UserId = id,
                DownloadedAt = DateTime.UtcNow,
                IPAddress = ip,
                UserAgent = agent
            });

            user.CvDownloadCount++;
            await _context.SaveChangesAsync();

            // Đọc file tĩnh PDF (HungYen_CV.pdf) từ thư mục wwwroot/uploads
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "HungYen_CV.pdf");

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Không tìm thấy file CV.");
            }

            var pdfBytes = await System.IO.File.ReadAllBytesAsync(filePath);

            return File(pdfBytes, "application/pdf", "CV_FULLSTACKDEVELOPER_VOHUNGYEN.pdf");
        }
    }
}