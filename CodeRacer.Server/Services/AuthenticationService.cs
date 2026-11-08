using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using CodeRacer.Server.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CodeRacer.Server.Services;

/// <summary>
/// JWT-based authentication service.
/// Generates tokens and handles password hashing with bcrypt.
/// Reads its settings from <see cref="JwtOptions"/> and the current time from <see cref="TimeProvider"/>,
/// so tests can supply both.
/// </summary>
public class AuthenticationService(IOptions<JwtOptions> jwtOptions, TimeProvider timeProvider) : IAuthenticationService
{
    private const int BcryptWorkFactor = 12; // Balance between security and performance

    private readonly JwtOptions _jwtOptions = jwtOptions.Value;
    private readonly TimeProvider _timeProvider = timeProvider;

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be empty", nameof(password));
        }

        return BCrypt.Net.BCrypt.HashPassword(password, BcryptWorkFactor);
    }

    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }

    public AuthenticationToken GenerateToken(Guid userId, string userName)
    {
        if (string.IsNullOrWhiteSpace(_jwtOptions.SecretKey))
        {
            throw new InvalidOperationException("JWT SecretKey is not configured");
        }

        var credentials = new SigningCredentials(_jwtOptions.CreateSigningKey(), SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, userName)
        };

        var issuedAt = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = issuedAt.Add(_jwtOptions.Expiration);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: issuedAt,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return new AuthenticationToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
