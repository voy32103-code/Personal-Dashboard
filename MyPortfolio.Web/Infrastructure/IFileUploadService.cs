namespace MyPortfolio.Web.Infrastructure;

/// <summary>
/// Abstraction cho file upload — tập trung toàn bộ logic validate & lưu file,
/// thay vì copy-paste sang Create.cshtml.cs và Edit.cshtml.cs (DRY).
/// </summary>
public interface IFileUploadService
{
    /// <summary>Validate và lưu ảnh. Trả về relative URL "/uploads/filename.ext"</summary>
    Task<(bool Success, string? RelativePath, string? ErrorMessage)> SaveImageAsync(
        IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>Validate và lưu audio. Trả về relative URL "/uploads/filename.ext"</summary>
    Task<(bool Success, string? RelativePath, string? ErrorMessage)> SaveAudioAsync(
        IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>Xóa file theo relative path (ví dụ "/uploads/abc.jpg"). An toàn với Path Traversal.</summary>
    void DeleteFile(string? relativePath);

    /// <summary>Rollback danh sách file đã upload (dùng khi DB save thất bại).</summary>
    void RollbackFiles(IEnumerable<string> relativePaths);
}
