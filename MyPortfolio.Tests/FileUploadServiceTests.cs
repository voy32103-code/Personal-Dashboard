using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using MyPortfolio.Web.Infrastructure;
using Xunit;

namespace MyPortfolio.Tests
{
    public class FileUploadServiceTests : IDisposable
    {
        private readonly string _testWebRoot;
        private readonly Mock<IWebHostEnvironment> _mockEnv;
        private readonly Mock<ILogger<FileUploadService>> _mockLogger;
        private readonly Mock<ICloudinary> _mockCloudinary;
        private readonly FileUploadService _service;

        public FileUploadServiceTests()
        {
            // Tạo thư mục WebRoot giả lập trong thư mục build để tránh ô nhiễm bên ngoài
            _testWebRoot = Path.Combine(Directory.GetCurrentDirectory(), "TestWebRoot_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testWebRoot);
            Directory.CreateDirectory(Path.Combine(_testWebRoot, "uploads"));

            _mockEnv = new Mock<IWebHostEnvironment>();
            _mockEnv.Setup(e => e.WebRootPath).Returns(_testWebRoot);

            _mockLogger = new Mock<ILogger<FileUploadService>>();
            _mockCloudinary = new Mock<ICloudinary>();

            _service = new FileUploadService(_mockEnv.Object, _mockLogger.Object, _mockCloudinary.Object);
        }

        public void Dispose()
        {
            // Dọn sạch thư mục giả lập sau mỗi bài test
            if (Directory.Exists(_testWebRoot))
            {
                Directory.Delete(_testWebRoot, true);
            }
        }

        private Mock<IFormFile> CreateMockFormFile(string fileName, long sizeBytes, byte[] content)
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(sizeBytes);
            fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(content));
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Callback<Stream, CancellationToken>((stream, token) =>
                {
                    stream.Write(content, 0, content.Length);
                })
                .Returns(Task.CompletedTask);

            return fileMock;
        }

        [Fact]
        public async Task SaveImageAsync_WithValidImage_ShouldSaveSuccessfully()
        {
            // Arrange
            var fileMock = CreateMockFormFile("avatar.png", 1024, new byte[] { 0x89, 0x50, 0x4E, 0x47 });
            var expectedUrl = "https://res.cloudinary.com/test-cloud/image/upload/v12345/portfolio/images/test-image.png";
            
            _mockCloudinary
                .Setup(c => c.UploadAsync(It.IsAny<ImageUploadParams>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ImageUploadResult { SecureUrl = new Uri(expectedUrl) });

            // Action
            var (success, path, error) = await _service.SaveImageAsync(fileMock.Object);

            // Assert
            Assert.True(success);
            Assert.Equal(expectedUrl, path);
            Assert.Null(error);
        }

        [Fact]
        public async Task SaveImageAsync_WithInvalidExtension_ShouldReturnError()
        {
            // Arrange
            var fileMock = CreateMockFormFile("malicious.exe", 1024, new byte[] { 0x4D, 0x5A });

            // Action
            var (success, path, error) = await _service.SaveImageAsync(fileMock.Object);

            // Assert
            Assert.False(success);
            Assert.Null(path);
            Assert.NotNull(error);
            Assert.Contains("Chỉ chấp nhận file ảnh", error);
        }

        [Fact]
        public async Task SaveImageAsync_WithFileTooLarge_ShouldReturnError()
        {
            // Arrange (11MB file)
            var fileMock = CreateMockFormFile("large.jpg", 11 * 1024 * 1024, new byte[0]);

            // Action
            var (success, path, error) = await _service.SaveImageAsync(fileMock.Object);

            // Assert
            Assert.False(success);
            Assert.Null(path);
            Assert.NotNull(error);
            Assert.Contains("không được vượt quá 10MB", error);
        }

        [Fact]
        public async Task SaveAudioAsync_WithValidAudio_ShouldSaveSuccessfully()
        {
            // Arrange
            var fileMock = CreateMockFormFile("music.mp3", 1024, new byte[] { 0x49, 0x44, 0x33 });
            var expectedUrl = "https://res.cloudinary.com/test-cloud/video/upload/v12345/portfolio/audio/test-audio.mp3";

            _mockCloudinary
                .Setup(c => c.UploadAsync(It.IsAny<VideoUploadParams>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new VideoUploadResult { SecureUrl = new Uri(expectedUrl) });

            // Action
            var (success, path, error) = await _service.SaveAudioAsync(fileMock.Object);

            // Assert
            Assert.True(success);
            Assert.Equal(expectedUrl, path);
            Assert.Null(error);
        }

        [Fact]
        public async Task SaveAudioAsync_WithFileTooLarge_ShouldReturnError()
        {
            // Arrange (21MB file)
            var fileMock = CreateMockFormFile("large_audio.mp3", 21L * 1024 * 1024, new byte[0]);

            // Action
            var (success, path, error) = await _service.SaveAudioAsync(fileMock.Object);

            // Assert
            Assert.False(success);
            Assert.Null(path);
            Assert.NotNull(error);
            Assert.Contains("không được vượt quá 20MB", error);
        }

        [Fact]
        public void DeleteFile_WithCloudinaryUrl_ShouldDeleteFromCloudinary()
        {
            // Arrange
            var cloudinaryUrl = "https://res.cloudinary.com/test-cloud/image/upload/v12345/portfolio/images/test-image.png";
            
            _mockCloudinary
                .Setup(c => c.Destroy(It.IsAny<DeletionParams>()))
                .Returns(new DeletionResult { Result = "ok" });

            // Action
            _service.DeleteFile(cloudinaryUrl);

            // Assert
            _mockCloudinary.Verify(
                c => c.Destroy(It.Is<DeletionParams>(p => p.PublicId == "portfolio/images/test-image" && p.ResourceType == ResourceType.Image)),
                Times.Once);
        }

        [Fact]
        public void DeleteFile_WithPathTraversalAttack_ShouldBlockDeletion()
        {
            // Arrange: Tạo một tệp nhạy cảm ngoài thư mục uploads (nằm ở WebRoot)
            var sensitiveFileName = "sensitive_config.json";
            var sensitiveFullPath = Path.Combine(_testWebRoot, sensitiveFileName);
            File.WriteAllText(sensitiveFullPath, "{ \"secret\": \"data\" }");
            Assert.True(File.Exists(sensitiveFullPath));

            // Đường dẫn tấn công Path Traversal nhằm xóa tệp ngoài thư mục uploads
            var traversalPath = "/uploads/../" + sensitiveFileName;

            // Action
            _service.DeleteFile(traversalPath);

            // Assert: File nhạy cảm vẫn phải an toàn, không bị xóa
            Assert.True(File.Exists(sensitiveFullPath));

            // Logger phải ghi nhận log lỗi bảo mật
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString() != null && v.ToString()!.Contains("Security: attempted to delete file outside uploads dir")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void RollbackFiles_ShouldDeleteAllUploadedFiles()
        {
            // Arrange
            var file1 = "https://res.cloudinary.com/test-cloud/image/upload/v12345/portfolio/images/file1.jpg";
            var file2 = "https://res.cloudinary.com/test-cloud/video/upload/v12345/portfolio/audio/file2.mp3";

            _mockCloudinary
                .Setup(c => c.Destroy(It.IsAny<DeletionParams>()))
                .Returns(new DeletionResult { Result = "ok" });

            // Action
            _service.RollbackFiles(new[] { file1, file2 });

            // Assert
            _mockCloudinary.Verify(
                c => c.Destroy(It.Is<DeletionParams>(p => p.PublicId == "portfolio/images/file1" && p.ResourceType == ResourceType.Image)),
                Times.Once);
            _mockCloudinary.Verify(
                c => c.Destroy(It.Is<DeletionParams>(p => p.PublicId == "portfolio/audio/file2" && p.ResourceType == ResourceType.Video)),
                Times.Once);
        }
    }
}
