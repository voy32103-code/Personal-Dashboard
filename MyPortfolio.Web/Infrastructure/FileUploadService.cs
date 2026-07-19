using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace MyPortfolio.Web.Infrastructure;

/// <summary>
/// Implementation của IFileUploadService sử dụng Cloudinary để lưu trữ vĩnh viễn.
/// </summary>
public class FileUploadService : IFileUploadService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileUploadService> _logger;
    private readonly ICloudinary _cloudinary;

    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly string[] AllowedAudioExtensions = { ".mp3", ".wav", ".ogg" };
    private const long MaxImageFileSizeBytes = 10 * 1024 * 1024; // 10MB
    private const long MaxAudioFileSizeBytes = 20 * 1024 * 1024; // 20MB

    public FileUploadService(IWebHostEnvironment environment, ILogger<FileUploadService> logger, ICloudinary cloudinary)
    {
        _environment = environment;
        _logger = logger;
        _cloudinary = cloudinary;
    }

    public async Task<(bool Success, string? RelativePath, string? ErrorMessage)> SaveImageAsync(
        IFormFile file, CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(ext))
            return (false, null, $"Chỉ chấp nhận file ảnh ({string.Join(", ", AllowedImageExtensions)}).");

        if (file.Length > MaxImageFileSizeBytes)
            return (false, null, "File ảnh không được vượt quá 10MB.");

        try
        {
            var fileName = Guid.NewGuid().ToString("N");
            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = "portfolio/images",
                PublicId = fileName,
                Overwrite = true
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary Image Upload Error: {Error}", uploadResult.Error.Message);
                return (false, null, "Không thể tải ảnh lên đám mây: " + uploadResult.Error.Message);
            }

            return (true, uploadResult.SecureUrl.ToString(), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload image to Cloudinary");
            return (false, null, "Lỗi hệ thống khi tải ảnh lên đám mây.");
        }
    }

    public async Task<(bool Success, string? RelativePath, string? ErrorMessage)> SaveAudioAsync(
        IFormFile file, CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedAudioExtensions.Contains(ext))
            return (false, null, $"Chỉ chấp nhận file âm thanh ({string.Join(", ", AllowedAudioExtensions)}).");

        if (file.Length > MaxAudioFileSizeBytes)
            return (false, null, "File audio không được vượt quá 20MB.");

        try
        {
            var fileName = Guid.NewGuid().ToString("N");
            using var stream = file.OpenReadStream();
            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = "portfolio/audio",
                PublicId = fileName,
                Overwrite = true
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary Audio Upload Error: {Error}", uploadResult.Error.Message);
                return (false, null, "Không thể tải nhạc lên đám mây: " + uploadResult.Error.Message);
            }

            return (true, uploadResult.SecureUrl.ToString(), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload audio to Cloudinary");
            return (false, null, "Lỗi hệ thống khi tải nhạc lên đám mây.");
        }
    }

    public void DeleteFile(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        if (relativePath.Contains("no-image.png", StringComparison.OrdinalIgnoreCase)) return;

        // Nếu là Cloudinary URL
        var cloudinaryInfo = ParseCloudinaryUrl(relativePath);
        if (cloudinaryInfo.HasValue)
        {
            try
            {
                var deletionParams = new DeletionParams(cloudinaryInfo.Value.PublicId)
                {
                    ResourceType = cloudinaryInfo.Value.ResourceType.Equals("video", StringComparison.OrdinalIgnoreCase) 
                        ? ResourceType.Video 
                        : ResourceType.Image
                };
                var result = _cloudinary.Destroy(deletionParams);
                if (result.Error != null)
                {
                    _logger.LogWarning("Cloudinary delete asset error: {Error}", result.Error.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete asset from Cloudinary: {Url}", relativePath);
            }
            return;
        }

        // Hỗ trợ xóa local files cũ (nếu có)
        if (!relativePath.StartsWith("/") ||
            relativePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return;

        var uploadsDir = Path.GetFullPath(Path.Combine(_environment.WebRootPath, "uploads"));
        var fullPath = Path.GetFullPath(Path.Combine(_environment.WebRootPath, relativePath.TrimStart('/')));

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

    private (string PublicId, string ResourceType)? ParseCloudinaryUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (!url.Contains("res.cloudinary.com", StringComparison.OrdinalIgnoreCase)) return null;

        try
        {
            var uri = new Uri(url);
            var segments = uri.Segments.Select(s => s.Trim('/')).Where(s => !string.IsNullOrEmpty(s)).ToList();

            var uploadIndex = segments.IndexOf("upload");
            if (uploadIndex == -1 || uploadIndex + 1 >= segments.Count) return null;

            var resourceType = segments[uploadIndex - 1]; // "image", "video", etc.

            var pathSegments = segments.Skip(uploadIndex + 1).ToList();
            if (pathSegments.Count > 0 && pathSegments[0].StartsWith("v") && double.TryParse(pathSegments[0].Substring(1), out _))
            {
                pathSegments = pathSegments.Skip(1).ToList();
            }

            if (pathSegments.Count == 0) return null;

            var lastSegment = pathSegments.Last();
            var dotIndex = lastSegment.LastIndexOf('.');
            if (dotIndex != -1)
            {
                pathSegments[pathSegments.Count - 1] = lastSegment.Substring(0, dotIndex);
            }

            var publicId = string.Join("/", pathSegments);
            return (publicId, resourceType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Cloudinary URL: {Url}", url);
            return null;
        }
    }
}
