namespace MyPortfolio.Web.Infrastructure;

/// <summary>
/// Implementation của IFileUploadService.
/// Đăng ký singleton/scoped trong Program.cs:
///     builder.Services.AddScoped&lt;IFileUploadService, FileUploadService&gt;();
/// </summary>
public class FileUploadService : IFileUploadService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileUploadService> _logger;

    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly string[] AllowedAudioExtensions = { ".mp3", ".wav", ".ogg" };
    private const long MaxImageFileSizeBytes = 10 * 1024 * 1024; // 10MB
    private const long MaxAudioFileSizeBytes = 20 * 1024 * 1024; // 20MB

    public FileUploadService(IWebHostEnvironment environment, ILogger<FileUploadService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<(bool Success, string? RelativePath, string? ErrorMessage)> SaveImageAsync(
        IFormFile file, CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(ext))
            return (false, null, $"Chỉ chấp nhận file ảnh ({string.Join(", ", AllowedImageExtensions)}).");

        if (file.Length > MaxImageFileSizeBytes)
            return (false, null, "File ảnh không được vượt quá 10MB.");

        return await SaveFileAsync(file, ext, cancellationToken);
    }

    public async Task<(bool Success, string? RelativePath, string? ErrorMessage)> SaveAudioAsync(
        IFormFile file, CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedAudioExtensions.Contains(ext))
            return (false, null, $"Chỉ chấp nhận file âm thanh ({string.Join(", ", AllowedAudioExtensions)}).");

        if (file.Length > MaxAudioFileSizeBytes)
            return (false, null, "File audio không được vượt quá 20MB.");

        return await SaveFileAsync(file, ext, cancellationToken);
    }

    public void DeleteFile(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        if (relativePath.Contains("no-image.png", StringComparison.OrdinalIgnoreCase)) return;

        if (!relativePath.StartsWith("/") ||
            relativePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return;

        var uploadsDir = Path.GetFullPath(Path.Combine(_environment.WebRootPath, "uploads"));
        var fullPath = Path.GetFullPath(Path.Combine(_environment.WebRootPath, relativePath.TrimStart('/')));

        // Chống Path Traversal — chỉ xóa file trong uploads/
        if (!fullPath.StartsWith(uploadsDir, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError("Security: attempted to delete file outside uploads dir: {Path}", relativePath);
            return;
        }

        try
        {
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file: {FilePath}", fullPath);
        }
    }

    public void RollbackFiles(IEnumerable<string> relativePaths)
    {
        foreach (var path in relativePaths)
        {
            DeleteFile(path);
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<(bool Success, string? RelativePath, string? ErrorMessage)> SaveFileAsync(
        IFormFile file, string ext, CancellationToken cancellationToken)
    {
        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        // Không dùng tên file gốc — tránh Path Traversal, ký tự đặc biệt
        var fileName = Guid.NewGuid().ToString("N") + ext;
        var fullPath = Path.Combine(uploadsFolder, fileName);

        try
        {
            // FileMode.CreateNew để chống race condition ghi đè
            await using var stream = new FileStream(fullPath, FileMode.CreateNew);
            await file.CopyToAsync(stream, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save uploaded file: {FileName}", fileName);
            return (false, null, "Lỗi hệ thống khi lưu file. Vui lòng thử lại.");
        }

        return (true, "/uploads/" + fileName, null);
    }
}
