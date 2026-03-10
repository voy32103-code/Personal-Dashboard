using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyPortfolio.Core.Entities;
using MyPortfolio.Infrastructure.Data;
using Microsoft.Extensions.Caching.Distributed; // THÊM DÒNG NÀY ĐỂ DÙNG CACHE
using QRCoder; // Thư viện QR
using QuestPDF.Fluent; // Thư viện PDF
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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
    [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
    public class ProfileModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache; // KHAI BÁO BIẾN CACHE

        public ProfileModel(ApplicationDbContext context, IDistributedCache cache) // TIÊM CACHE VÀO CONSTRUCTOR
        {
            _context = context;
            _cache = cache; // GÁN GIÁ TRỊ CACHE
            // Cấu hình bản quyền miễn phí (Community) cho thư viện QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public User ProfileUser { get; set; } = default!;
        public int LinesOfCode { get; set; } = 15800;
        public string GithubUrl { get; set; } = "https://github.com/vohungyen";
        public List<SkillItem> Skills { get; set; } = new();

        // Giữ lại các biến này để file HTML giao diện (Razor) của bạn không bị vỡ lỗi
        public string FullName => ProfileUser?.Name ?? "VÕ HƯNG YÊN";
        public string Title => ProfileUser?.Summary ?? "SE Student @ HUFLIT • .NET Full-Stack Developer";
        public int UserId => ProfileUser?.Id ?? 1;

        // --- 1. LẤY THÔNG TIN KHI LOAD TRANG ---
        public async Task<IActionResult> OnGetAsync(int id = 1) // Tạm mặc định lấy ID 1
        {
            var user = await _context.Users.FindAsync(id);
            var cachedSkills = await _cache.GetStringAsync("skills"); // BÂY GIỜ DÒNG NÀY SẼ HOẠT ĐỘNG
            // Nếu Database trống trơn, tự động tạo 1 Profile ảo để bạn test
            if (user == null)
            {
                user = new User
                {
                    Name = "VÕ HƯNG YÊN",
                    Email = "contact@vohungyen.com",
                    Phone = "0901234567",
                    Summary = "SE Student @ HUFLIT • .NET Full-Stack Developer",
                    CvDownloadCount = 0,
                    QrScanCount = 0
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            ProfileUser = user;

            // Nạp danh sách kỹ năng
            Skills = new List<SkillItem>
            {
                new SkillItem { Name = ".NET 8 & System Architecture", Description = "Razor Pages, SignalR", Icon = "fab fa-microsoft", Color = "text-primary", Badge = "Core", BadgeColor = "bg-primary", Progress = 95 },
                new SkillItem { Name = "Database & EF Core", Description = "Neon PostgreSQL & LINQ", Icon = "fas fa-database", Color = "text-info", Badge = "Data", BadgeColor = "bg-info", Progress = 90 },
                new SkillItem { Name = "Redis Distributed Caching", Description = "Cache-Aside, Performance", Icon = "fas fa-bolt", Color = "text-warning", Badge = "Speed", BadgeColor = "bg-warning text-dark", Progress = 85 },
                new SkillItem { Name = "Security & OAuth 2.0", Description = "Google Auth, Anti-Traversal", Icon = "fas fa-shield-alt", Color = "text-danger", Badge = "Sec", BadgeColor = "bg-danger", Progress = 88 }
            };

            return Page();
        }

        // --- 2. TẠO MÃ QR CODE ẢNH THEO ID USER ---
        public IActionResult OnGetQrImage(int id)
        {
            // Link khi người ta quét mã QR sẽ trỏ về Handler ScanQr bên dưới
            var scanUrl = $"{Request.Scheme}://{Request.Host}/Profile?handler=ScanQr&id={id}";

            using var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(scanUrl, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrData);
            var qrBytes = qrCode.GetGraphic(20);

            return File(qrBytes, "image/png"); // Trả về dạng ảnh
        }

        // --- 3. THEO DÕI LOG KHI CÓ NGƯỜI QUÉT MÃ QR ---
        public async Task<IActionResult> OnGetScanQrAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            // Lưu lịch sử quét
            _context.QrScanLogs.Add(new QrScanLog
            {
                UserId = id,
                ScannedAt = DateTime.UtcNow,
                IPAddress = ip
            });

            user.QrScanCount++;
            await _context.SaveChangesAsync();

            // Chuyển hướng người quét về trang Profile bình thường
            return RedirectToPage("/Profile", new { id = id });
        }

        // --- 4. TẢI CV, VẼ PDF VÀ THEO DÕI LOG DOWNLOAD ---
        public async Task<IActionResult> OnGetDownloadCvAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var agent = Request.Headers["User-Agent"].ToString();

            // Lưu lịch sử tải
            _context.DownloadLogs.Add(new DownloadLog
            {
                UserId = id,
                DownloadedAt = DateTime.UtcNow,
                IPAddress = ip,
                UserAgent = agent
            });

            user.CvDownloadCount++;
            await _context.SaveChangesAsync();

            // 🎨 DÙNG C# ĐỂ VẼ RA FILE PDF ĐỘNG DỰA TRÊN DATABASE
            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Content().Column(col =>
                    {
                        col.Item().Text(user.Name).FontSize(28).Bold().FontColor(Colors.Green.Darken2);
                        col.Item().Text($"Email: {user.Email}").FontSize(14);
                        col.Item().Text($"Phone: {user.Phone}").FontSize(14);

                        col.Item().PaddingTop(20).Text("Professional Summary").FontSize(18).Bold().FontColor(Colors.Grey.Darken3);
                        col.Item().Text(user.Summary).FontSize(12);

                        col.Item().PaddingTop(30).Text($"System generated on {DateTime.Now:dd/MM/yyyy}").FontSize(10).Italic().FontColor(Colors.Grey.Medium);
                    });
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", $"{user.Name.Replace(" ", "_")}_Live_CV.pdf");
        }

    }
}