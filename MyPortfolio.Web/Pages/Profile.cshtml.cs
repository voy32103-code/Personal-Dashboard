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
        public string Title { get; set; } = ".NET Developer • System Architect";
        public int LinesOfCode { get; set; } = 12500; // Số dòng code ước tính của dự án này
        public string GithubUrl { get; set; } = "https://github.com/vohungyen";

        public List<SkillItem> Skills { get; set; } = new List<SkillItem>();

        public void OnGet()
        {
            // CẬP NHẬT: Tech Stack thực tế của dự án Portfolio này
            Skills = new List<SkillItem>
            {
                new SkillItem {
                    Name = ".NET 8 & SignalR",
                    Description = "Real-time Audio Sync & MVC Core",
                    Icon = "fab fa-microsoft",
                    Color = "text-white",
                    Badge = "Core",
                    BadgeColor = "bg-primary",
                    Progress = 100
                },
                new SkillItem {
                    Name = "PostgreSQL & EF Core",
                    Description = "Neon Tech Cloud DB & LINQ Queries",
                    Icon = "fas fa-database",
                    Color = "text-info",
                    Badge = "Data",
                    BadgeColor = "bg-info",
                    Progress = 95
                },
                new SkillItem {
                    Name = "Security & OAuth 2.0",
                    Description = "Google Authentication & Identity",
                    Icon = "fas fa-shield-alt",
                    Color = "text-danger",
                    Badge = "Sec",
                    BadgeColor = "bg-danger",
                    Progress = 90
                },
                new SkillItem {
                    Name = "DevOps (Docker/Render)",
                    Description = "CI/CD Pipeline & Containerization",
                    Icon = "fab fa-docker",
                    Color = "text-warning",
                    Badge = "Ops",
                    BadgeColor = "bg-warning text-dark",
                    Progress = 85
                }
            };
        }
    }
}