namespace MyPortfolio.Web.Infrastructure;

/// <summary>
/// Nguồn chân lý duy nhất cho tất cả Redis cache keys.
/// Khi thay đổi format key, chỉ cần sửa ở đây — không còn bị out-of-sync giữa các Page Models.
/// </summary>
public static class CacheKeys
{
    // --- Home / Index ---
    public static string HomeProjects(string mode, string search)
        => $"home_projects:{mode}:{search}";

    public const string HomeProjectsNormal = "home_projects:normal:none";
    public const string HomeProjectsLibrary = "home_projects:library:none";

    // --- Dashboard ---
    // Bump version suffix khi thay đổi DashboardStats schema
    public const string DashboardStats = "admin_dashboard_stats_v2";

    // --- Profile ---
    public const string ProfileSkills = "profile_skills_v1";
}
