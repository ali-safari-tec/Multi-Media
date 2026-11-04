using MediaServer.Api.Models.Auth;
using MediaServer.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MediaServer.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly D1Service _d1Service;

        public AuthController(
            AuthService authService,
            D1Service d1Service)
        {
            _authService = authService;
            _d1Service = d1Service;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                return BadRequest(new { success = false, message = "Username is required." });

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { success = false, message = "Password is required." });

            // Normalize input values.
            var username = request.Username.Trim();

            var email = string.IsNullOrWhiteSpace(request.Email)
                ? null
                : request.Email.Trim();

            var phoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
                ? null
                : request.PhoneNumber.Trim();

            if (username.Length < 3 || username.Length > 50)
                return BadRequest(new { success = false, message = "Username must be between 3 and 50 characters." });

            if (request.Password.Length < 8 || request.Password.Length > 128)
                return BadRequest(new { success = false, message = "Password must be between 8 and 128 characters." });

            if (email == null && phoneNumber == null)
                return BadRequest(new { success = false, message = "Email or phone number is required." });

            // Check for duplicate username, email or phone number.
            using var checkResult = await _d1Service.QueryAsync(
                """
        SELECT UserId
        FROM Users
        WHERE Name = ?1
           OR (?2 IS NOT NULL AND Gmail = ?2)
           OR (?3 IS NOT NULL AND PhoneNumber = ?3)
        LIMIT 1;
        """,
                new object?[]
                {
            username,
            email,
            phoneNumber
                },
                cancellationToken);

            var results = checkResult.RootElement
                .GetProperty("result")[0]
                .GetProperty("results");

            if (results.GetArrayLength() > 0)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Username, email or phone number already exists."
                });
            }

            // Hash the password.
            var passwordHash = _authService.HashPassword(request.Password);

            // Store the creation date in UTC ISO 8601 format.
            var createdAt = DateTime.UtcNow.ToString("O");

            // Insert the new user into D1.
            using var insertResult = await _d1Service.QueryAsync(
                """
        INSERT INTO Users
        (
            Name,
            Gmail,
            PhoneNumber,
            AccessLevel,
            Password,
            AcountCreatedDate,
            SubscriptionDate
        )
        VALUES (?1, ?2, ?3, 3, ?4, ?5, NULL);
        """,
                new object?[]
                {
            username,
            email,
            phoneNumber,
            passwordHash,
            createdAt
                },
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, new
            {
                success = true,
                message = "Registration completed successfully."
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            var username = request.Username?.Trim() ?? "";
            var password = request.Password;

            if (username.Length < 3 ||
                username.Length > 50 ||
                string.IsNullOrEmpty(password) ||
                password.Length > 128)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid username or password."
                });
            }

            // Find user.
            using var userResult = await _d1Service.QueryAsync(
                """
        SELECT
            UserId,
            Name,
            Password,
            AccessLevel,
            SubscriptionDate
        FROM Users
        WHERE Name = ?1
        LIMIT 1;
        """,
                new object?[] { username },
                cancellationToken);

            var users = userResult.RootElement
                .GetProperty("result")[0]
                .GetProperty("results");

            if (users.GetArrayLength() == 0)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid username or password."
                });
            }

            var user = users[0];

            var storedPassword = user.GetProperty("Password").GetString();

            if (storedPassword == null ||
                !_authService.VerifyPassword(password, storedPassword))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid username or password."
                });
            }

            // Generate session and tokens.
            var sessionId = _authService.GenerateSessionId();

            var accessToken = _authService.GenerateToken();
            var refreshToken = _authService.GenerateToken();

            var accessTokenHash = _authService.HashToken(accessToken);
            var refreshTokenHash = _authService.HashToken(refreshToken);

            var now = DateTime.UtcNow;

            var accessTokenExpiresAt = now.AddMinutes(15);

            var expiresAt = now.AddDays(request.RememberMe ? 30 : 1);

            var deviceName = string.IsNullOrWhiteSpace(request.DeviceName)
                ? null
                : request.DeviceName.Trim();

            if (deviceName?.Length > 100)
                deviceName = deviceName[..100];

            var platform = string.IsNullOrWhiteSpace(request.Platform)
                ? null
                : request.Platform.Trim();

            if (platform?.Length > 50)
                platform = platform[..50];

            // Create session in D1.
            using var insertResult = await _d1Service.QueryAsync(
                """
        INSERT INTO UserSessions (
            SessionId,
            UserId,
            RefreshTokenHash,
            AccessTokenHash,
            AccessTokenExpiresAt,
            DeviceName,
            Platform,
            RememberMe,
            CreatedAt,
            LastUsedAt,
            ExpiresAt
        )
        VALUES (
            ?1, ?2, ?3, ?4, ?5,
            ?6, ?7, ?8, ?9, ?10, ?11
        );
        """,
                new object?[]
                {
            sessionId,
            user.GetProperty("UserId").GetInt32(),
            refreshTokenHash,
            accessTokenHash,
            accessTokenExpiresAt.ToString("O"),
            deviceName,
            platform,
            request.RememberMe ? 1 : 0,
            now.ToString("O"),
            now.ToString("O"),
            expiresAt.ToString("O")
                },
                cancellationToken);

            var changes = insertResult.RootElement
    .GetProperty("result")[0]
    .GetProperty("meta")
    .GetProperty("changes")
    .GetInt32();

            if (changes != 1)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "Failed to create session."
                });
            }

            return Ok(new
            {
                success = true,
                message = "Login successful",
                tokens = new
                {
                    accessToken,
                    refreshToken,
                    accessTokenExpiresAt,
                    expiresAt
                },
                user = new
                {
                    userId = user.GetProperty("UserId").GetInt32(),
                    username = user.GetProperty("Name").GetString(),
                    accessLevel = user.GetProperty("AccessLevel").GetInt32(),
                    subscriptionDate = user.GetProperty("SubscriptionDate").ValueKind
                        == System.Text.Json.JsonValueKind.Null
                            ? null
                            : user.GetProperty("SubscriptionDate").GetString()
                }
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
        {
            var refreshToken = request.RefreshToken;

            if (string.IsNullOrWhiteSpace(refreshToken) ||
                refreshToken.Length < 32)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid refresh token."
                });
            }

            var refreshTokenHash = _authService.HashToken(refreshToken);

            // Find session by refresh token.
            using var sessionResult = await _d1Service.QueryAsync(
                """
        SELECT
            SessionId,
            UserId,
            RememberMe,
            ExpiresAt
        FROM UserSessions
        WHERE RefreshTokenHash = ?1
        LIMIT 1;
        """,
                new object?[] { refreshTokenHash },
                cancellationToken);

            var sessions = sessionResult.RootElement
                .GetProperty("result")[0]
                .GetProperty("results");

            if (sessions.GetArrayLength() == 0)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid or expired refresh token."
                });
            }

            var session = sessions[0];

            var sessionId = session.GetProperty("SessionId").GetString()!;
            var userId = session.GetProperty("UserId").GetInt32();
            var rememberMe = session.GetProperty("RememberMe").GetInt32() == 1;

            var expiresAtText = session.GetProperty("ExpiresAt").GetString();

            if (!DateTime.TryParse(
                    expiresAtText,
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out var expiresAt))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid or expired refresh token."
                });
            }

            var now = DateTime.UtcNow;

            // Delete expired session.
            if (expiresAt <= now)
            {
                using var deleteResult = await _d1Service.QueryAsync(
                    """
            DELETE FROM UserSessions
            WHERE SessionId = ?1;
            """,
                    new object?[] { sessionId },
                    cancellationToken);

                return Unauthorized(new
                {
                    success = false,
                    message = "Session has expired. Please login again."
                });
            }

            // Generate new tokens.
            var newAccessToken = _authService.GenerateToken();
            var newRefreshToken = _authService.GenerateToken();

            var newAccessTokenHash = _authService.HashToken(newAccessToken);
            var newRefreshTokenHash = _authService.HashToken(newRefreshToken);

            var accessTokenExpiresAt = now.AddMinutes(15);
            var newSessionExpiresAt = now.AddDays(1);

            // Update session and rotate tokens.
            using var updateResult = await _d1Service.QueryAsync(
                """
        UPDATE UserSessions
        SET
            RefreshTokenHash = ?1,
            AccessTokenHash = ?2,
            AccessTokenExpiresAt = ?3,
            LastUsedAt = ?4,
            ExpiresAt = CASE
                WHEN RememberMe = 0 THEN ?5
                ELSE ExpiresAt
            END
        WHERE SessionId = ?6
          AND RefreshTokenHash = ?7
          AND ExpiresAt > ?8;
        """,
                new object?[]
                {
            newRefreshTokenHash,
            newAccessTokenHash,
            accessTokenExpiresAt.ToString("O"),
            now.ToString("O"),
            newSessionExpiresAt.ToString("O"),
            sessionId,
            refreshTokenHash,
            now.ToString("O")
                },
                cancellationToken);

            var changes = updateResult.RootElement
    .GetProperty("result")[0]
    .GetProperty("meta")
    .GetProperty("changes")
    .GetInt32();

            if (changes != 1)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Session expired or refresh token has already been used."
                });
            }

            // Return new tokens.
            return Ok(new
            {
                success = true,
                message = "Token refreshed successfully.",
                tokens = new
                {
                    accessToken = newAccessToken,
                    refreshToken = newRefreshToken,
                    accessTokenExpiresAt,
                    expiresAt = rememberMe
                        ? expiresAt
                        : newSessionExpiresAt
                }
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest request, CancellationToken cancellationToken)
        {
            var refreshToken = request.RefreshToken;

            if (string.IsNullOrWhiteSpace(refreshToken) ||
                refreshToken.Length < 32)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid refresh token."
                });
            }

            var refreshTokenHash = _authService.HashToken(refreshToken);

            using var result = await _d1Service.QueryAsync(
                """
        DELETE FROM UserSessions
        WHERE RefreshTokenHash = ?1;
        """,
                new object?[] { refreshTokenHash },
                cancellationToken);

            return Ok(new
            {
                success = true,
                message = "Logout successful."
            });
        }

        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] ValidateRequest request, CancellationToken cancellationToken)
        {
            var accessToken = request.AccessToken;

            if (string.IsNullOrWhiteSpace(accessToken) ||
                accessToken.Length < 32)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid access token."
                });
            }

            var accessTokenHash = _authService.HashToken(accessToken);

            using var result = await _d1Service.QueryAsync(
                """
        SELECT
            U.UserId,
            U.Name,
            U.AccessLevel,
            U.SubscriptionDate
        FROM UserSessions S
        INNER JOIN Users U ON U.UserId = S.UserId
        WHERE S.AccessTokenHash = ?1
          AND S.AccessTokenExpiresAt > ?2
          AND S.ExpiresAt > ?2
        LIMIT 1;
        """,
                new object?[]
                {
            accessTokenHash,
            DateTime.UtcNow.ToString("O")
                },
                cancellationToken);

            var users = result.RootElement
                .GetProperty("result")[0]
                .GetProperty("results");

            if (users.GetArrayLength() == 0)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Access token is invalid or expired."
                });
            }

            var user = users[0];

            return Ok(new
            {
                success = true,
                message = "Access token is valid.",
                user = new
                {
                    userId = user.GetProperty("UserId").GetInt32(),
                    username = user.GetProperty("Name").GetString(),
                    accessLevel = user.GetProperty("AccessLevel").GetInt32(),
                    subscriptionDate =
                        user.GetProperty("SubscriptionDate").ValueKind
                            == System.Text.Json.JsonValueKind.Null
                            ? null
                            : user.GetProperty("SubscriptionDate").GetString()
                }
            });
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
        {
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

            if (accessToken.Length < 32)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid access token."
                });
            }

            var accessTokenHash = _authService.HashToken(accessToken);
            var now = DateTime.UtcNow.ToString("O");

            // Find the current session.
            using var currentSessionResult = await _d1Service.QueryAsync(
                """
        SELECT SessionId, UserId
        FROM UserSessions
        WHERE AccessTokenHash = ?1
          AND AccessTokenExpiresAt > ?2
          AND ExpiresAt > ?2
        LIMIT 1;
        """,
                new object?[] { accessTokenHash, now },
                cancellationToken);

            var currentSessions = currentSessionResult.RootElement
                .GetProperty("result")[0]
                .GetProperty("results");

            if (currentSessions.GetArrayLength() == 0)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Access token is invalid or expired."
                });
            }

            var currentSession = currentSessions[0];
            var currentSessionId = currentSession
                .GetProperty("SessionId").GetString();

            var userId = currentSession.GetProperty("UserId").GetInt32();

            // Get all active sessions of this user.
            using var sessionsResult = await _d1Service.QueryAsync(
                """
        SELECT
            SessionId,
            DeviceName,
            Platform,
            RememberMe,
            CreatedAt,
            LastUsedAt,
            ExpiresAt
        FROM UserSessions
        WHERE UserId = ?1
          AND ExpiresAt > ?2
        ORDER BY LastUsedAt DESC;
        """,
                new object?[] { userId, now },
                cancellationToken);

            var sessions = sessionsResult.RootElement
                .GetProperty("result")[0]
                .GetProperty("results");

            var sessionList = new List<object>();

            foreach (var session in sessions.EnumerateArray())
            {
                sessionList.Add(new
                {
                    sessionId = session.GetProperty("SessionId").GetString(),
                    deviceName = session.GetProperty("DeviceName").ValueKind
                        == System.Text.Json.JsonValueKind.Null
                            ? null
                            : session.GetProperty("DeviceName").GetString(),
                    platform = session.GetProperty("Platform").ValueKind
                        == System.Text.Json.JsonValueKind.Null
                            ? null
                            : session.GetProperty("Platform").GetString(),
                    rememberMe = session.GetProperty("RememberMe").GetInt32() == 1,
                    createdAt = session.GetProperty("CreatedAt").GetString(),
                    lastUsedAt = session.GetProperty("LastUsedAt").GetString(),
                    expiresAt = session.GetProperty("ExpiresAt").GetString(),
                    isCurrent = session.GetProperty("SessionId").GetString()
                        == currentSessionId
                });
            }

            return Ok(new
            {
                success = true,
                sessions = sessionList
            });
        }

        [HttpPost("revoke-session")]
        public async Task<IActionResult> RevokeSession([FromBody] RevokeSessionRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.SessionId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Session ID is required."
                });
            }

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

            if (accessToken.Length < 32)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid access token."
                });
            }

            var accessTokenHash = _authService.HashToken(accessToken);
            var now = DateTime.UtcNow.ToString("O");

            // Find the current session.
            using var currentSessionResult = await _d1Service.QueryAsync(
                """
        SELECT SessionId, UserId
        FROM UserSessions
        WHERE AccessTokenHash = ?1
          AND AccessTokenExpiresAt > ?2
          AND ExpiresAt > ?2
        LIMIT 1;
        """,
                new object?[] { accessTokenHash, now },
                cancellationToken);

            var currentSessions = currentSessionResult.RootElement
                .GetProperty("result")[0]
                .GetProperty("results");

            if (currentSessions.GetArrayLength() == 0)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Access token is invalid or expired."
                });
            }

            var currentSession = currentSessions[0];

            var currentSessionId =
                currentSession.GetProperty("SessionId").GetString();

            var userId = currentSession.GetProperty("UserId").GetInt32();

            // Prevent revoking the current session.
            if (request.SessionId == currentSessionId)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "You cannot revoke the current session."
                });
            }

            // Delete only the requested session belonging to this user.
            using var deleteResult = await _d1Service.QueryAsync(
                """
        DELETE FROM UserSessions
        WHERE SessionId = ?1
          AND UserId = ?2
          AND ExpiresAt > ?3;
        """,
                new object?[]
                {
            request.SessionId,
            userId,
            now
                },
                cancellationToken);

            return Ok(new
            {
                success = true,
                message = "Session revoked successfully."
            });
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.CurrentPassword) ||
                string.IsNullOrEmpty(request.NewPassword))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Current password and new password are required."
                });
            }

            if (request.NewPassword.Length < 8 ||
                request.NewPassword.Length > 128)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "New password must be between 8 and 128 characters."
                });
            }

            // Get access token from Authorization header.
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

            if (accessToken.Length < 32)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid access token."
                });
            }

            var accessTokenHash = _authService.HashToken(accessToken);
            var now = DateTime.UtcNow.ToString("O");

            // Find the authenticated user.
            using var userResult = await _d1Service.QueryAsync(
                """
        SELECT
            U.UserId,
            U.Password
        FROM UserSessions S
        INNER JOIN Users U ON U.UserId = S.UserId
        WHERE S.AccessTokenHash = ?1
          AND S.AccessTokenExpiresAt > ?2
          AND S.ExpiresAt > ?2
        LIMIT 1;
        """,
                new object?[] { accessTokenHash, now },
                cancellationToken);

            var users = userResult.RootElement
                .GetProperty("result")[0]
                .GetProperty("results");

            if (users.GetArrayLength() == 0)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Access token is invalid or expired."
                });
            }

            var user = users[0];

            var userId = user.GetProperty("UserId").GetInt32();
            var storedPassword = user.GetProperty("Password").GetString();

            // Verify current password.
            if (storedPassword == null ||
                !_authService.VerifyPassword(
                    request.CurrentPassword,
                    storedPassword))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Current password is incorrect."
                });
            }

            // Hash the new password.
            var newPasswordHash = _authService.HashPassword(
                request.NewPassword);

            // Update password without deleting any sessions.
            using var updateResult = await _d1Service.QueryAsync(
                """
        UPDATE Users
        SET Password = ?1
        WHERE UserId = ?2
          AND Password = ?3;
        """,
                new object?[]
                {
            newPasswordHash,
            userId,
            storedPassword
                },
                cancellationToken);

            var changes = updateResult.RootElement
                .GetProperty("result")[0]
                .GetProperty("meta")
                .GetProperty("changes")
                .GetInt32();

            if (changes != 1)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Password could not be updated. Please try again."
                });
            }

            return Ok(new
            {
                success = true,
                message = "Password changed successfully."
            });
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me(CancellationToken cancellationToken)
        {
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

            if (accessToken.Length < 32)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid access token."
                });
            }

            var accessTokenHash = _authService.HashToken(accessToken);
            var now = DateTime.UtcNow.ToString("O");

            // Get user and current session information.
            using var result = await _d1Service.QueryAsync(
                """
        SELECT
            U.UserId,
            U.Name,
            U.Gmail,
            U.PhoneNumber,
            U.AccessLevel,
            U.Picture,
            U.AcountCreatedDate,
            U.SubscriptionDate,
            S.SessionId,
            S.DeviceName,
            S.Platform,
            S.RememberMe,
            S.CreatedAt,
            S.LastUsedAt,
            S.ExpiresAt
        FROM UserSessions S
        INNER JOIN Users U ON U.UserId = S.UserId
        WHERE S.AccessTokenHash = ?1
          AND S.AccessTokenExpiresAt > ?2
          AND S.ExpiresAt > ?2
        LIMIT 1;
        """,
                new object?[] { accessTokenHash, now },
                cancellationToken);

            var users = result.RootElement
                .GetProperty("result")[0]
                .GetProperty("results");

            if (users.GetArrayLength() == 0)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Access token is invalid or expired."
                });
            }

            var user = users[0];

            var sessionId = user.GetProperty("SessionId").GetString();

            // Update LastUsedAt for the current session.
            using var updateResult = await _d1Service.QueryAsync(
                """
        UPDATE UserSessions
        SET LastUsedAt = ?1
        WHERE SessionId = ?2
          AND AccessTokenHash = ?3;
        """,
                new object?[]
                {
            now,
            sessionId,
            accessTokenHash
                },
                cancellationToken);

            return Ok(new
            {
                success = true,

                user = new
                {
                    userId = user.GetProperty("UserId").GetInt32(),
                    username = user.GetProperty("Name").GetString(),

                    email = user.GetProperty("Gmail").ValueKind
                        == System.Text.Json.JsonValueKind.Null
                            ? null
                            : user.GetProperty("Gmail").GetString(),

                    phoneNumber = user.GetProperty("PhoneNumber").ValueKind
                        == System.Text.Json.JsonValueKind.Null
                            ? null
                            : user.GetProperty("PhoneNumber").GetString(),

                    accessLevel = user.GetProperty("AccessLevel").GetInt32(),

                    picture = user.GetProperty("Picture").ValueKind
                        == System.Text.Json.JsonValueKind.Null
                            ? null
                            : user.GetProperty("Picture").GetString(),

                    accountCreatedDate =
                        user.GetProperty("AcountCreatedDate").GetString(),

                    subscriptionDate = user.GetProperty("SubscriptionDate").ValueKind
                        == System.Text.Json.JsonValueKind.Null
                            ? null
                            : user.GetProperty("SubscriptionDate").GetString()
                },

                session = new
                {
                    sessionId,
                    deviceName = user.GetProperty("DeviceName").ValueKind
                        == System.Text.Json.JsonValueKind.Null
                            ? null
                            : user.GetProperty("DeviceName").GetString(),

                    platform = user.GetProperty("Platform").ValueKind
                        == System.Text.Json.JsonValueKind.Null
                            ? null
                            : user.GetProperty("Platform").GetString(),

                    rememberMe = user.GetProperty("RememberMe").GetInt32() == 1,
                    createdAt = user.GetProperty("CreatedAt").GetString(),
                    lastUsedAt = now,
                    expiresAt = user.GetProperty("ExpiresAt").GetString()
                }
            });
        }

        [HttpPatch("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
        {
            // Get access token.
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

            if (accessToken.Length < 32)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid access token."
                });
            }

            var accessTokenHash = _authService.HashToken(accessToken);
            var now = DateTime.UtcNow.ToString("O");

            // Authenticate user.
            using var userResult = await _d1Service.QueryAsync(
                """
        SELECT U.UserId, U.Name, U.Gmail, U.PhoneNumber, U.Picture
        FROM UserSessions S
        INNER JOIN Users U ON U.UserId = S.UserId
        WHERE S.AccessTokenHash = ?1
          AND S.AccessTokenExpiresAt > ?2
          AND S.ExpiresAt > ?2
        LIMIT 1;
        """,
                new object?[] { accessTokenHash, now },
                cancellationToken);

            var users = userResult.RootElement
                .GetProperty("result")[0]
                .GetProperty("results");

            if (users.GetArrayLength() == 0)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Access token is invalid or expired."
                });
            }

            var user = users[0];

            var userId = user.GetProperty("UserId").GetInt32();

            var currentUsername = user.GetProperty("Name").GetString()!;
            var currentEmail = user.GetProperty("Gmail").ValueKind
                == System.Text.Json.JsonValueKind.Null
                    ? null
                    : user.GetProperty("Gmail").GetString();

            var currentPhone = user.GetProperty("PhoneNumber").ValueKind
                == System.Text.Json.JsonValueKind.Null
                    ? null
                    : user.GetProperty("PhoneNumber").GetString();

            var currentPicture = user.GetProperty("Picture").ValueKind
                == System.Text.Json.JsonValueKind.Null
                    ? null
                    : user.GetProperty("Picture").GetString();

            // Keep existing values when a property is omitted.
            var username = request.Username == null
                ? currentUsername
                : request.Username.Trim();

            var email = request.Email == null
                ? currentEmail
                : (string.IsNullOrWhiteSpace(request.Email)
                    ? null
                    : request.Email.Trim());

            var phoneNumber = request.PhoneNumber == null
                ? currentPhone
                : (string.IsNullOrWhiteSpace(request.PhoneNumber)
                    ? null
                    : request.PhoneNumber.Trim());

            var picture = request.Picture == null
                ? currentPicture
                : (string.IsNullOrWhiteSpace(request.Picture)
                    ? null
                    : request.Picture.Trim());

            // Validate username.
            if (username.Length < 3 || username.Length > 50)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Username must be between 3 and 50 characters."
                });
            }

            // Validate email or phone number.
            if (email == null && phoneNumber == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Email or phone number is required."
                });
            }

            // Check duplicate username, email and phone number.
            using var duplicateResult = await _d1Service.QueryAsync(
                """
        SELECT UserId
        FROM Users
        WHERE UserId != ?1
          AND (
              Name = ?2
              OR (?3 IS NOT NULL AND Gmail = ?3)
              OR (?4 IS NOT NULL AND PhoneNumber = ?4)
          )
        LIMIT 1;
        """,
                new object?[]
                {
            userId,
            username,
            email,
            phoneNumber
                },
                cancellationToken);

            var duplicates = duplicateResult.RootElement
                .GetProperty("result")[0]
                .GetProperty("results");

            if (duplicates.GetArrayLength() > 0)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Username, email or phone number already exists."
                });
            }

            // Update profile.
            using var updateResult = await _d1Service.QueryAsync(
                """
        UPDATE Users
        SET
            Name = ?1,
            Gmail = ?2,
            PhoneNumber = ?3,
            Picture = ?4
        WHERE UserId = ?5;
        """,
                new object?[]
                {
            username,
            email,
            phoneNumber,
            picture,
            userId
                },
                cancellationToken);

            var changes = updateResult.RootElement
    .GetProperty("result")[0]
    .GetProperty("meta")
    .GetProperty("changes")
    .GetInt32();

            if (changes != 1)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Profile update failed. Please try again."
                });
            }

            // Update current session activity.
            using var sessionUpdateResult = await _d1Service.QueryAsync(
                """
        UPDATE UserSessions
        SET LastUsedAt = ?1
        WHERE SessionId = (
            SELECT SessionId
            FROM UserSessions
            WHERE AccessTokenHash = ?2
            LIMIT 1
        );
        """,
                new object?[]
                {
            now,
            accessTokenHash
                },
                cancellationToken);

            return Ok(new
            {
                success = true,
                message = "Profile updated successfully.",
                user = new
                {
                    userId,
                    username,
                    email,
                    phoneNumber,
                    picture
                }
            });
        }
    }
}
