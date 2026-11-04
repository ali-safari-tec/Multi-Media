using MediaServer.Api.Models.Common;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace MediaServer.Api.Services;

public class AuthService
{
    private const int PasswordIterations = 100000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    // Generate a secure random token
    public string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    // Hash token using SHA256 and standard Base64
    public string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);

        return Convert.ToBase64String(hash);
    }

    // Generate a UUID for a session
    public string GenerateSessionId()
    {
        return Guid.NewGuid().ToString("D");
    }

    // Hash password using PBKDF2-SHA256
    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            PasswordIterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return $"pbkdf2$sha256${PasswordIterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    // Verify password against stored hash
    public bool VerifyPassword(string password, string storedPassword)
    {
        try
        {
            var parts = storedPassword.Split('$');

            if (parts.Length != 5 ||
                parts[0] != "pbkdf2" ||
                parts[1] != "sha256" ||
                !int.TryParse(parts[2], out var iterations) ||
                iterations < 1 || iterations > 100000)
            {
                return false;
            }

            var salt = Convert.FromBase64String(parts[3]);
            var storedHash = Convert.FromBase64String(parts[4]);

            if (salt.Length != SaltSize ||
                storedHash.Length != HashSize)
            {
                return false;
            }

            var calculatedHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                HashSize);

            return CryptographicOperations.FixedTimeEquals(
                calculatedHash,
                storedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public async Task<AuthenticatedUser?> ValidateAccessTokenAsync(string accessToken, D1Service d1Service, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken) ||
            accessToken.Length < 32)
        {
            return null;
        }

        var accessTokenHash = HashToken(accessToken);
        var now = DateTime.UtcNow.ToString("O");

        using var result = await d1Service.QueryAsync(
            """
        SELECT
            U.UserId,
            U.Name,
            U.AccessLevel,
            S.SessionId
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
            now
            },
            cancellationToken);

        var users = result.RootElement
            .GetProperty("result")[0]
            .GetProperty("results");

        if (users.GetArrayLength() == 0)
            return null;

        var user = users[0];

        return new AuthenticatedUser
        {
            UserId = user.GetProperty("UserId").GetInt32(),
            Username = user.GetProperty("Name").GetString() ?? "",
            AccessLevel = user.GetProperty("AccessLevel").GetInt32(),
            SessionId = user.GetProperty("SessionId").GetString() ?? ""
        };
    }

}
