using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace MyPortfolio.Web.Pages
{
    public class SkillItem
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public string Badge { get; set; }
        public string BadgeColor { get; set; }
        public int Progress { get; set; }
    }

    public class ProfileModel : PageModel
    {
        public string FullName { get; set; } = "VÕ HƯNG YÊN";

        // Cập nhật chức danh sát với thực tế để gây ấn tượng với nhà tuyển dụng
        public string Title { get; set; } = "SE Student @ HUFLIT • .NET Full-Stack Developer";

        // Tăng số dòng code lên chút vì nãy giờ fix bug quá trời! 😆
        public int LinesOfCode { get; set; } = 15800;
        public string GithubUrl { get; set; } = "https://github.com/vohungyen";

        public List<SkillItem> Skills { get; set; } = new List<SkillItem>();

        public void OnGet()
        {
            // CẬP NHẬT: Tech Stack thực tế bao gồm những thứ bạn vừa tự tay code
            Skills = new List<SkillItem>
            {
                new SkillItem {
                    Name = ".NET 8 & System Architecture",
                    Description = "Razor Pages, SignalR & Clean Code",
                    Icon = "fab fa-microsoft",
                    Color = "text-primary",
                    Badge = "Core",
                    BadgeColor = "bg-primary",
                    Progress = 95
                },
                new SkillItem {
                    Name = "Database & EF Core",
                    Description = "Neon PostgreSQL & LINQ Optimization",
                    Icon = "fas fa-database",
                    Color = "text-info",
                    Badge = "Data",
                    BadgeColor = "bg-info",
                    Progress = 90
                },
                new SkillItem {
                    Name = "Redis Distributed Caching",
                    Description = "Cache-Aside, Performance & Invalidation",
                    Icon = "fas fa-bolt", // Icon tia sét thể hiện tốc độ
                    Color = "text-warning",
                    Badge = "Speed",
                    BadgeColor = "bg-warning text-dark",
                    Progress = 85
                },
                new SkillItem {
                    Name = "Security & OAuth 2.0",
                    Description = "Google Auth, Anti-Traversal & Concurrency",
                    Icon = "fas fa-shield-alt",
                    Color = "text-danger",
                    Badge = "Sec",
                    BadgeColor = "bg-danger",
                    Progress = 88
                },
               
            };
        }
    }
}