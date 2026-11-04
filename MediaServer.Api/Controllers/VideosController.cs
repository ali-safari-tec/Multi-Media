using MediaServer.Api.Models.Videos;
using MediaServer.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Globalization;


namespace MediaServer.Api.Controllers
{
    [ApiController]
    [Route("api/videos")]
    public class VideosController : ControllerBase
    {
        private readonly VideoService _videoService;
        private readonly AuthService _authService;
        private readonly D1Service _d1Service;
        private readonly ILogger<VideosController> _logger;

        public VideosController(VideoService videoService, AuthService authService, D1Service d1Service, ILogger<VideosController> logger)
        {
            _videoService = videoService;
            _authService = authService;
            _d1Service = d1Service;
            _logger = logger;
        }

        [HttpGet("{videoId:int}/file")]
        public async Task<IActionResult> GetVideo(int videoId, CancellationToken cancellationToken)
        {
            // 1. Validate Access Token.
            var authorization = Request.Headers.Authorization.ToString();

            if (!authorization.StartsWith(
                "Bearer ",
                StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Access token is required."
                });
            }

            var accessToken = authorization["Bearer ".Length..].Trim();

            var user = await _authService.ValidateAccessTokenAsync(
                accessToken,
                _d1Service,
                cancellationToken);

            if (user == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Access token is invalid or expired."
                });
            }

            // 2. Only Admin and active subscribers can access video files.
            if (user.AccessLevel != 1 && user.AccessLevel != 2)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    success = false,
                    message = "You do not have permission to access this video."
                });
            }

            // 3. Validate VideoId.
            if (videoId <= 0)
                return BadRequest("Invalid video ID.");

            try
            {
                // 4. Retrieve the stored file name from D1.
                const string sql = """
            SELECT StoredName
            FROM Videos
            WHERE VideoId = ?1
            LIMIT 1;
            """;

                using var result = await _d1Service.QueryAsync(
                    sql,
                    new object?[]
                    {
                videoId
                    },
                    cancellationToken);

                var videos = result.RootElement
                    .GetProperty("result")[0]
                    .GetProperty("results");

                if (videos.GetArrayLength() == 0)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Video not found."
                    });
                }

                var storedName = videos[0]
                    .GetProperty("StoredName")
                    .GetString();

                if (string.IsNullOrWhiteSpace(storedName))
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Video file not found."
                    });
                }

                // 5. Validate the stored file path.
                string filePath;

                try
                {
                    filePath = _videoService.GetSafeFilePath(storedName);
                }
                catch (ArgumentException)
                {
                    _logger.LogError(
                        "Invalid stored file name for VideoId={VideoId}",
                        videoId);

                    return NotFound("Video file not found.");
                }

                // 6. Check whether the physical file exists.
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Video file not found."
                    });
                }

                // 7. Stream video with Range support.
                var contentType = _videoService.GetContentType(storedName);

                return PhysicalFile(
                    filePath,
                    contentType,
                    enableRangeProcessing: true);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to stream video. VideoId={VideoId}",
                    videoId);

                return Problem(
                    "Failed to retrieve video file.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetVideos([FromQuery] string? search, [FromQuery] string? publisherName, [FromQuery] int? type, [FromQuery] int? seriesId, [FromQuery] int? resolution, [FromQuery] string? format, [FromQuery] string? dateFrom, [FromQuery] string? dateTo, [FromQuery] int limit = 50, [FromQuery] int offset = 0, CancellationToken cancellationToken = default)
        {
            // 1. Validate Access Token.
            var authorization = Request.Headers.Authorization.ToString();

            if (!authorization.StartsWith(
                "Bearer ",
                StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Access token is required."
                });
            }

            var accessToken = authorization["Bearer ".Length..].Trim();

            var user = await _authService.ValidateAccessTokenAsync(
                accessToken,
                _d1Service,
                cancellationToken);

            if (user == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Access token is invalid or expired."
                });
            }

            // 2. Validate pagination parameters.
            if (limit < 1 || limit > 100)
                return BadRequest("Limit must be between 1 and 100.");

            if (offset < 0)
                return BadRequest("Offset cannot be negative.");

            // 3. Validate filter parameters.
            if (type.HasValue && (type.Value < 1 || type.Value > 4))
                return BadRequest("Invalid video type.");

            if (seriesId.HasValue && seriesId.Value <= 0)
                return BadRequest("Invalid series ID.");

            if (resolution.HasValue && resolution.Value <= 0)
                return BadRequest("Invalid resolution.");

            // 4. Normalize text filters.
            search = string.IsNullOrWhiteSpace(search)
                ? null
                : search.Trim();

            publisherName = string.IsNullOrWhiteSpace(publisherName)
                ? null
                : publisherName.Trim();

            format = string.IsNullOrWhiteSpace(format)
                ? null
                : format.Trim();

            // 5. Validate publication date filters.
            if (!string.IsNullOrWhiteSpace(dateFrom) &&
                !DateTime.TryParseExact(
                    dateFrom,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out _))
            {
                return BadRequest("dateFrom must use yyyy-MM-dd format.");
            }

            if (!string.IsNullOrWhiteSpace(dateTo) &&
                !DateTime.TryParseExact(
                    dateTo,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out _))
            {
                return BadRequest("dateTo must use yyyy-MM-dd format.");
            }

            if (!string.IsNullOrWhiteSpace(dateFrom) &&
                !string.IsNullOrWhiteSpace(dateTo) &&
                string.CompareOrdinal(dateFrom, dateTo) > 0)
            {
                return BadRequest("dateFrom cannot be later than dateTo.");
            }

            dateFrom = string.IsNullOrWhiteSpace(dateFrom) ? null : dateFrom;
            dateTo = string.IsNullOrWhiteSpace(dateTo) ? null : dateTo;

            try
            {
                // 6. Retrieve filtered video metadata from D1.
                const string sql = """
            SELECT
                VideoId,
                SeriesId,
                Name,
                PublisherName,
                PublicationDate,
                Size,
                Resolution,
                Format,
                Synopsis,
                Thumbnail,
                Type
            FROM Videos
            WHERE
                (
                    ?1 IS NULL
                    OR Name LIKE '%' || ?1 || '%'
                    OR OrginalName LIKE '%' || ?1 || '%'
                    OR PublisherName LIKE '%' || ?1 || '%'
                    OR Synopsis LIKE '%' || ?1 || '%'
                )
                AND (
                    ?2 IS NULL
                    OR PublisherName LIKE '%' || ?2 || '%'
                )
                AND (?3 IS NULL OR Type = ?3)
                AND (?4 IS NULL OR SeriesId = ?4)
                AND (?5 IS NULL OR Resolution = ?5)
                AND (
                    ?6 IS NULL
                    OR LOWER(Format) = LOWER(?6)
                )
                AND (
                    ?7 IS NULL
                    OR PublicationDate >= ?7
                )
                AND (
                    ?8 IS NULL
                    OR PublicationDate <= ?8
                )
            ORDER BY VideoId DESC
            LIMIT ?9 OFFSET ?10;
            """;

                using var result = await _d1Service.QueryAsync(
                    sql,
                    new object?[]
                    {
                search,
                publisherName,
                type,
                seriesId,
                resolution,
                format,
                dateFrom,
                dateTo,
                limit,
                offset
                    },
                    cancellationToken);

                // 7. Extract and clone the returned rows.
                var videos = result.RootElement
                    .GetProperty("result")[0]
                    .GetProperty("results")
                    .Clone();

                return Ok(new
                {
                    success = true,
                    count = videos.GetArrayLength(),
                    videos
                });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to retrieve filtered video list.");

                return Problem(
                    "Failed to retrieve videos.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("{videoId:int}")]
        public async Task<IActionResult> GetVideoDetails(int videoId, CancellationToken cancellationToken)
        {
            // 1. Validate Access Token.
            var authorization = Request.Headers.Authorization.ToString();

            if (!authorization.StartsWith(
                "Bearer ",
                StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Access token is required."
                });
            }

            var accessToken = authorization["Bearer ".Length..].Trim();

            var user = await _authService.ValidateAccessTokenAsync(
                accessToken,
                _d1Service,
                cancellationToken);

            if (user == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Access token is invalid or expired."
                });
            }

            // 2. Validate VideoId.
            if (videoId <= 0)
                return BadRequest("Invalid video ID.");

            try
            {
                // 3. Retrieve video details from D1.
                const string sql = """
            SELECT
                VideoId,
                SeriesId,
                Name,
                PublisherName,
                PublicationDate,
                Size,
                Resolution,
                Format,
                Synopsis,
                Thumbnail,
                Type
            FROM Videos
            WHERE VideoId = ?1
            LIMIT 1;
            """;

                using var result = await _d1Service.QueryAsync(
                    sql,
                    new object?[]
                    {
                videoId
                    },
                    cancellationToken);

                var videos = result.RootElement
                    .GetProperty("result")[0]
                    .GetProperty("results");

                // 4. Check whether the video exists.
                if (videos.GetArrayLength() == 0)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Video not found."
                    });
                }

                var video = videos[0].Clone();

                return Ok(new
                {
                    success = true,
                    video
                });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to retrieve video details. VideoId={VideoId}",
                    videoId);

                return Problem(
                    "Failed to retrieve video details.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("upload-file")]
        [RequestSizeLimit(20L * 1024 * 1024 * 1024)]
        public async Task<IActionResult> UploadFile([FromQuery] string? fileName, CancellationToken cancellationToken)
        {
            // 1. Validate Access Token.
            var authorization = Request.Headers.Authorization.ToString();

            if (!authorization.StartsWith(
                "Bearer ",
                StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Access token is required."
                });
            }

            var accessToken = authorization["Bearer ".Length..].Trim();

            var user = await _authService.ValidateAccessTokenAsync(
                accessToken,
                _d1Service,
                cancellationToken);

            if (user == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Access token is invalid or expired."
                });
            }

            // 2. Only Admin users can upload videos.
            if (user.AccessLevel != 1)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    success = false,
                    message = "You do not have permission to upload videos."
                });
            }

            // 3. Validate request content type.
            if (!MediaTypeHeaderValue.TryParse(
                Request.ContentType,
                out var contentType) ||
                !string.Equals(
                    contentType.MediaType.Value,
                    "application/octet-stream",
                    StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(
                    StatusCodes.Status415UnsupportedMediaType,
                    "Content-Type must be application/octet-stream.");
            }

            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest("File name is required.");

            // Handle Windows-style paths.
            var originalFileName = Path.GetFileName(
                fileName.Replace('\\', '/'));

            if (string.IsNullOrWhiteSpace(originalFileName))
                return BadRequest("Invalid file name.");

            var extension = Path.GetExtension(originalFileName);

            if (!_videoService.IsAllowedExtension(extension))
                return BadRequest("Unsupported video extension.");

            if (Request.ContentLength == 0)
                return BadRequest("Empty upload.");

            (string FileName, long Size)? savedVideo = null;
            bool keepFile = false;

            try
            {
                _logger.LogInformation(
                    "Video upload started. OriginalName={OriginalName}, ContentLength={ContentLength}",
                    originalFileName,
                    Request.ContentLength);

                // 4. Save the video directly from the request body.
                savedVideo = await _videoService.SaveVideoAsync(
                    Request.Body,
                    extension,
                    cancellationToken);

                _logger.LogInformation(
                    "Video uploaded successfully. StoredName={StoredName}, Size={Size}",
                    savedVideo.Value.FileName,
                    savedVideo.Value.Size);

                keepFile = true;

                return Ok(new
                {
                    success = true,
                    fileName = savedVideo.Value.FileName,
                    originalName = originalFileName,
                    size = savedVideo.Value.Size
                });
            }
            catch (InvalidOperationException ex)
                when (ex.Message.Contains(
                    "maximum allowed size",
                    StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(
                    StatusCodes.Status413PayloadTooLarge);
            }
            catch (InvalidOperationException ex)
                when (ex.Message == "Empty upload.")
            {
                return BadRequest("Empty upload.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Video file upload failed.");

                return Problem(
                    "Video file upload failed.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            finally
            {
                if (savedVideo.HasValue && !keepFile)
                {
                    try
                    {
                        _videoService.DeleteVideo(
                            savedVideo.Value.FileName);
                    }
                    catch (Exception cleanupException)
                    {
                        _logger.LogError(
                            cleanupException,
                            "Failed to delete video after unsuccessful upload: {FileName}",
                            savedVideo.Value.FileName);
                    }
                }
            }
        }

        [HttpPost("save-metadata")]
        public async Task<IActionResult> SaveMetadata([FromBody] SaveVideoMetadataRequest request, CancellationToken cancellationToken)
        {
            // 1. Validate Access Token.
            var authorization = Request.Headers.Authorization.ToString();

            if (!authorization.StartsWith(
                "Bearer ",
                StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Access token is required."
                });
            }

            var accessToken = authorization["Bearer ".Length..].Trim();

            var user = await _authService.ValidateAccessTokenAsync(
                accessToken,
                _d1Service,
                cancellationToken);

            if (user == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Access token is invalid or expired."
                });
            }

            // 2. Only Admin users can save video metadata.
            if (user.AccessLevel != 1)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    success = false,
                    message = "You do not have permission to save video metadata."
                });
            }

            // 3. Validate the uploaded file name.
            if (string.IsNullOrWhiteSpace(request.FileName))
                return BadRequest("File name is required.");

            string filePath;

            try
            {
                filePath = _videoService.GetSafeFilePath(request.FileName);
            }
            catch (ArgumentException)
            {
                return BadRequest("Invalid file name.");
            }

            if (!System.IO.File.Exists(filePath))
                return NotFound("Uploaded video file not found.");

            bool keepFile = false;

            try
            {
                // 4. Validate original file name.
                if (string.IsNullOrWhiteSpace(request.OriginalName))
                    return BadRequest("Original file name is required.");

                var originalName = Path.GetFileName(
                    request.OriginalName.Replace('\\', '/'));

                if (string.IsNullOrWhiteSpace(originalName))
                    return BadRequest("Invalid original file name.");

                // 5. Validate file extension.
                var extension = Path.GetExtension(request.FileName);

                if (!_videoService.IsAllowedExtension(extension))
                    return BadRequest("Unsupported video extension.");

                // 6. Validate metadata.
                if (string.IsNullOrWhiteSpace(request.Name))
                    return BadRequest("Video name is required.");

                if (request.Type < 1 || request.Type > 4)
                    return BadRequest("Invalid video type.");

                if (request.Resolution <= 0)
                    return BadRequest("Invalid video resolution.");

                if (request.SeriesId.HasValue && request.SeriesId.Value <= 0)
                    return BadRequest("Invalid series ID.");

                if (!string.IsNullOrWhiteSpace(request.PublicationDate) &&
                    !DateTime.TryParseExact(
                        request.PublicationDate,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out _))
                {
                    return BadRequest("Publication date must use yyyy-MM-dd format.");
                }

                // 7. Read the actual file size from disk.
                var fileSize = new FileInfo(filePath).Length;

                // 8. Insert metadata into Cloudflare D1.
                const string sql = """
            INSERT INTO Videos (
                SeriesId,
                Name,
                OrginalName,
                StoredName,
                PublisherName,
                PublicationDate,
                Size,
                Resolution,
                Format,
                Synopsis,
                Type
            )
            VALUES (
                ?1, ?2, ?3, ?4, ?5, ?6,
                ?7, ?8, ?9, ?10, ?11
            );
            """;

                using var result = await _d1Service.QueryAsync(
                    sql,
                    new object?[]
                    {
                request.SeriesId,
                request.Name.Trim(),
                originalName,
                request.FileName,
                request.PublisherName,
                request.PublicationDate,
                fileSize,
                request.Resolution,
                extension.TrimStart('.').ToUpperInvariant(),
                request.Synopsis,
                request.Type
                    },
                    cancellationToken);

                // 9. Verify that exactly one row was inserted.
                var changes = result.RootElement
                    .GetProperty("result")[0]
                    .GetProperty("meta")
                    .GetProperty("changes")
                    .GetInt32();

                if (changes != 1)
                {
                    throw new InvalidOperationException(
                        "Video metadata was not inserted into D1.");
                }

                keepFile = true;

                _logger.LogInformation(
                    "Video metadata saved successfully. StoredName={StoredName}",
                    request.FileName);

                return Ok(new
                {
                    success = true,
                    message = "Video metadata saved successfully."
                });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to save video metadata. StoredName={StoredName}",
                    request.FileName);

                return Problem(
                    "Failed to save video metadata.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            finally
            {
                // 10. Delete the uploaded file if metadata saving failed.
                if (!keepFile)
                {
                    try
                    {
                        _videoService.DeleteVideo(request.FileName);

                        _logger.LogWarning(
                            "Uploaded video deleted because metadata saving failed. StoredName={StoredName}",
                            request.FileName);
                    }
                    catch (Exception cleanupException)
                    {
                        _logger.LogCritical(
                            cleanupException,
                            "CRITICAL: Failed to delete video after metadata saving failure. StoredName={StoredName}",
                            request.FileName);
                    }
                }
            }
        }

        [HttpDelete("delete-file/{fileName}")]
        public async Task<IActionResult> DeleteUploadedFile(string fileName, CancellationToken cancellationToken)
        {
            // 1. Validate Access Token.
            var authorization = Request.Headers.Authorization.ToString();

            if (!authorization.StartsWith(
                "Bearer ",
                StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Access token is required."
                });
            }

            var accessToken = authorization["Bearer ".Length..].Trim();

            var user = await _authService.ValidateAccessTokenAsync(
                accessToken,
                _d1Service,
                cancellationToken);

            if (user == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Access token is invalid or expired."
                });
            }

            // 2. Only Admin users can delete uploaded files.
            if (user.AccessLevel != 1)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    success = false,
                    message = "You do not have permission to delete video files."
                });
            }

            // 3. Validate file name.
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest("File name is required.");

            string filePath;

            try
            {
                filePath = _videoService.GetSafeFilePath(fileName);
            }
            catch (ArgumentException)
            {
                return BadRequest("Invalid file name.");
            }

            // 4. Delete file if it exists.
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    _videoService.DeleteVideo(fileName);
                }

                return Ok(new
                {
                    success = true,
                    message = "Video file deleted successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to delete video file. FileName={FileName}",
                    fileName);

                return Problem(
                    "Failed to delete video file.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPut("update-metadata/{videoId:int}")]
        public async Task<IActionResult> UpdateMetadata(int videoId, [FromBody] UpdateVideoMetadataRequest request, CancellationToken cancellationToken)
        {
            // 1. Validate Access Token.
            var authorization = Request.Headers.Authorization.ToString();

            if (!authorization.StartsWith(
                "Bearer ",
                StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Access token is required."
                });
            }

            var accessToken = authorization["Bearer ".Length..].Trim();

            var user = await _authService.ValidateAccessTokenAsync(
                accessToken,
                _d1Service,
                cancellationToken);

            if (user == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Access token is invalid or expired."
                });
            }

            // 2. Only Admin users can update video metadata.
            if (user.AccessLevel != 1)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    success = false,
                    message = "You do not have permission to update video metadata."
                });
            }

            // 3. Validate VideoId.
            if (videoId <= 0)
                return BadRequest("Invalid video ID.");

            // 4. Validate metadata.
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Video name is required.");

            if (request.Type < 1 || request.Type > 4)
                return BadRequest("Invalid video type.");

            if (request.Resolution <= 0)
                return BadRequest("Invalid video resolution.");

            if (request.SeriesId.HasValue && request.SeriesId.Value <= 0)
                return BadRequest("Invalid series ID.");

            if (!string.IsNullOrWhiteSpace(request.PublicationDate) &&
                !DateTime.TryParseExact(
                    request.PublicationDate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out _))
            {
                return BadRequest("Publication date must use yyyy-MM-dd format.");
            }

            // 5. Normalize optional fields.
            var publisherName = string.IsNullOrWhiteSpace(request.PublisherName)
                ? null
                : request.PublisherName.Trim();

            var publicationDate = string.IsNullOrWhiteSpace(request.PublicationDate)
                ? null
                : request.PublicationDate.Trim();

            var synopsis = string.IsNullOrWhiteSpace(request.Synopsis)
                ? null
                : request.Synopsis.Trim();

            var thumbnail = string.IsNullOrWhiteSpace(request.Thumbnail)
                ? null
                : request.Thumbnail.Trim();

            try
            {
                // 6. Update metadata in Cloudflare D1.
                const string sql = """
            UPDATE Videos
            SET
                SeriesId = ?1,
                Name = ?2,
                PublisherName = ?3,
                PublicationDate = ?4,
                Resolution = ?5,
                Synopsis = ?6,
                Thumbnail = ?7,
                Type = ?8
            WHERE VideoId = ?9;
            """;

                using var result = await _d1Service.QueryAsync(
                    sql,
                    new object?[]
                    {
                request.SeriesId,
                request.Name.Trim(),
                publisherName,
                publicationDate,
                request.Resolution,
                synopsis,
                thumbnail,
                request.Type,
                videoId
                    },
                    cancellationToken);

                // 7. Verify that a row was updated.
                var changes = result.RootElement
                    .GetProperty("result")[0]
                    .GetProperty("meta")
                    .GetProperty("changes")
                    .GetInt32();

                if (changes == 0)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Video not found."
                    });
                }

                _logger.LogInformation(
                    "Video metadata updated successfully. VideoId={VideoId}",
                    videoId);

                return Ok(new
                {
                    success = true,
                    message = "Video metadata updated successfully."
                });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update video metadata. VideoId={VideoId}",
                    videoId);

                return Problem(
                    "Failed to update video metadata.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpDelete("{videoId:int}")]
        public async Task<IActionResult> DeleteVideo(int videoId, CancellationToken cancellationToken)
        {
            // 1. Validate Access Token.
            var authorization = Request.Headers.Authorization.ToString();

            if (!authorization.StartsWith(
                "Bearer ",
                StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Access token is required."
                });
            }

            var accessToken = authorization["Bearer ".Length..].Trim();

            var user = await _authService.ValidateAccessTokenAsync(
                accessToken,
                _d1Service,
                cancellationToken);

            if (user == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Access token is invalid or expired."
                });
            }

            // 2. Only Admin users can delete videos.
            if (user.AccessLevel != 1)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    success = false,
                    message = "You do not have permission to delete videos."
                });
            }

            // 3. Validate VideoId.
            if (videoId <= 0)
                return BadRequest("Invalid video ID.");

            try
            {
                // 4. Retrieve the stored file name from D1.
                const string selectSql = """
            SELECT StoredName
            FROM Videos
            WHERE VideoId = ?1
            LIMIT 1;
            """;

                using var selectResult = await _d1Service.QueryAsync(
                    selectSql,
                    new object?[]
                    {
                videoId
                    },
                    cancellationToken);

                var videos = selectResult.RootElement
                    .GetProperty("result")[0]
                    .GetProperty("results");

                if (videos.GetArrayLength() == 0)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Video not found."
                    });
                }

                var storedName = videos[0]
                    .GetProperty("StoredName")
                    .GetString();

                if (string.IsNullOrWhiteSpace(storedName))
                {
                    _logger.LogError(
                        "StoredName is missing for VideoId={VideoId}",
                        videoId);

                    return Problem(
                        "Video file information is invalid.",
                        statusCode: StatusCodes.Status500InternalServerError);
                }

                // 5. Validate the physical file path before deleting metadata.
                try
                {
                    _videoService.GetSafeFilePath(storedName);
                }
                catch (ArgumentException)
                {
                    _logger.LogError(
                        "Invalid stored file name for VideoId={VideoId}",
                        videoId);

                    return Problem(
                        "Video file information is invalid.",
                        statusCode: StatusCodes.Status500InternalServerError);
                }

                // 6. Delete video metadata from D1.
                const string deleteSql = """
            DELETE FROM Videos
            WHERE VideoId = ?1;
            """;

                using var deleteResult = await _d1Service.QueryAsync(
                    deleteSql,
                    new object?[]
                    {
                videoId
                    },
                    cancellationToken);

                var changes = deleteResult.RootElement
                    .GetProperty("result")[0]
                    .GetProperty("meta")
                    .GetProperty("changes")
                    .GetInt32();

                if (changes != 1)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Video not found."
                    });
                }

                // 7. Delete the physical file from VPS.
                try
                {
                    _videoService.DeleteVideo(storedName);

                    _logger.LogInformation(
                        "Video deleted successfully. VideoId={VideoId}",
                        videoId);

                    return Ok(new
                    {
                        success = true,
                        metadataDeleted = true,
                        fileDeleted = true,
                        message = "Video deleted successfully."
                    });
                }
                catch (Exception fileException)
                {
                    _logger.LogCritical(
                        fileException,
                        "Video metadata was deleted, but physical file cleanup failed. VideoId={VideoId}, StoredName={StoredName}",
                        videoId,
                        storedName);

                    return Problem(
                        "Video metadata was deleted, but its physical file could not be removed from the server.",
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to delete video. VideoId={VideoId}",
                    videoId);

                return Problem(
                    "Failed to delete video.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}
