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
using MyPortfolio.Web.Infrastructure;

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
            // M-5: QuestPDF.Settings.License được set trong Program.cs, không cần set lại ở đây
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
        // C-4: Không hardcode id != 1 — lấy user đầu tiên trong DB (sau khi seed)
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _context.Users
                .OrderBy(u => u.Id)
                .FirstOrDefaultAsync();

            // Skills từ cache (dùng CacheKeys constant để nhất quán)
            var cachedSkills = await _cache.GetStringAsync(CacheKeys.ProfileSkills);
            if (cachedSkills != null)
            {
                Skills = JsonSerializer.Deserialize<List<SkillItem>>(cachedSkills) ?? BuildDefaultSkills();
            }
            else
            {
                Skills = BuildDefaultSkills();
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                };
                await _cache.SetStringAsync(CacheKeys.ProfileSkills, JsonSerializer.Serialize(Skills), cacheOptions);
            }

            // Nếu không tìm thấy user (seed chưa chạy), dùng fallback tạm
            if (user == null)
            {
                user = new User
                {
                    Name = "VÕ HƯNG YÊN",
                    Summary = "SE Student @ HUFLIT • .NET Full-Stack Developer",
                    AvatarPath = "/images/no-image.png",
                };
                // KHÔNG gọi SaveChangesAsync() trong GET handler — seed được xử lý trong Program.cs
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
        public async Task<IActionResult> OnPostDownloadCvAsync()
        {
            // C-4: Không nhận id từ URL — luôn lấy profile user (tránh manipulation)
            var user = await _context.Users.OrderBy(u => u.Id).FirstOrDefaultAsync();
            if (user == null) return NotFound();

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = Request.Headers["User-Agent"].ToString();

            // Ghi Log vào DB
            _context.DownloadLogs.Add(new DownloadLog
            {
                UserId = user.Id,
                DownloadedAt = DateTime.UtcNow,
                IPAddress = ip,
                UserAgent = userAgent
            });
            await _context.SaveChangesAsync();

            // Tăng bộ đếm tải CV nguyên tử
            await _context.Users
                .Where(u => u.Id == user.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.CvDownloadCount, u => u.CvDownloadCount + 1));

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