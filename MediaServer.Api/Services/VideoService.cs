
using Microsoft.AspNetCore.Http.Features;

namespace MediaServer.Api.Services
{
    public class VideoService
    {
        private const long MaxUploadBytes =
            20L * 1024 * 1024 * 1024;

        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".mp4", ".mpg", ".mpeg", ".webm",
                ".mov", ".avi", ".mkv", ".wmv", ".avchd"
            };

        private readonly IConfiguration _configuration;
        private readonly ILogger<VideoService> _logger;

        public VideoService(
            IConfiguration configuration,
            ILogger<VideoService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public long GetMaxUploadBytes()
        {
            return MaxUploadBytes;
        }

        public string? GetVideoFolder()
        {
            return _configuration["MediaStorage:VideoFolder"];
        }

        public bool IsAllowedExtension(string extension)
        {
            return AllowedExtensions.Contains(extension);
        }

        public string GetContentType(string fileName)
        {
            return Path.GetExtension(fileName).ToLowerInvariant()
                switch
            {
                ".mp4" => "video/mp4",
                ".mpg" or ".mpeg" => "video/mpeg",
                ".webm" => "video/webm",
                ".mov" => "video/quicktime",
                ".avi" => "video/x-msvideo",
                ".mkv" => "video/x-matroska",
                ".wmv" => "video/x-ms-wmv",
                ".avchd" => "application/octet-stream",
                _ => "application/octet-stream"
            };
        }

        public string GetSafeFilePath(string fileName)
        {
            var videoFolder = GetVideoFolder();

            if (string.IsNullOrWhiteSpace(videoFolder))
            {
                throw new InvalidOperationException(
                    "MediaStorage:VideoFolder is not configured.");
            }

            var safeFileName = Path.GetFileName(fileName);

            if (!string.Equals(
                safeFileName,
                fileName,
                StringComparison.Ordinal))
            {
                throw new ArgumentException("Invalid file name.");
            }

            return Path.Combine(videoFolder, safeFileName);
        }

        public async Task<(string FileName, long Size)> SaveVideoAsync(Stream inputStream, string extension, CancellationToken cancellationToken)
        {
            if (!IsAllowedExtension(extension))
                throw new ArgumentException("Unsupported video extension.");

            var videoFolder = GetVideoFolder();

            if (string.IsNullOrWhiteSpace(videoFolder))
            {
                throw new InvalidOperationException(
                    "Video storage is not configured.");
            }

            Directory.CreateDirectory(videoFolder);

            var storedFileName =
                $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

            var tempFileName = $"{storedFileName}.uploading";

            var tempFilePath = Path.Combine(videoFolder, tempFileName);
            var finalFilePath = Path.Combine(videoFolder, storedFileName);

            long totalBytes = 0;

            try
            {
                await using (var output = new FileStream(
                    tempFilePath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    options: FileOptions.Asynchronous |
                             FileOptions.SequentialScan))
                {
                    var buffer = new byte[81920];
                    int bytesRead;

                    while ((bytesRead = await inputStream.ReadAsync(
                        buffer.AsMemory(),
                        cancellationToken)) > 0)
                    {
                        totalBytes += bytesRead;

                        if (totalBytes > MaxUploadBytes)
                        {
                            throw new InvalidOperationException(
                                "Upload exceeds the maximum allowed size.");
                        }

                        await output.WriteAsync(
                            buffer.AsMemory(0, bytesRead),
                            cancellationToken);
                    }

                    if (totalBytes == 0)
                    {
                        throw new InvalidOperationException(
                            "Empty upload.");
                    }

                    await output.FlushAsync(cancellationToken);
                }

                // Move the completed file to its final name.
                File.Move(tempFilePath, finalFilePath);

                return (storedFileName, totalBytes);
            }
            catch
            {
                try
                {
                    if (File.Exists(tempFilePath))
                        File.Delete(tempFilePath);
                }
                catch (Exception cleanupException)
                {
                    _logger.LogError(
                        cleanupException,
                        "Failed to delete incomplete upload: {FilePath}",
                        tempFilePath);
                }

                throw;
            }
        }

        public bool VideoExists(string fileName)
        {
            var filePath = GetSafeFilePath(fileName);

            return File.Exists(filePath);
        }

        public void DeleteVideo(string fileName)
        {
            var filePath = GetSafeFilePath(fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
