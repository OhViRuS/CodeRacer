using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CodeRacer.Server.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace CodeRacer.Server.Services;

/// <summary>
/// JWT-based authentication service.
/// Generates tokens and handles password hashing with bcrypt.
/// </summary>
public class AuthenticationService(IConfiguration configuration) : IAuthenticationService
{
    private readonly IConfiguration _configuration = configuration;
    private const int BcryptWorkFactor = 12; // Balance between security and performance

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
        catch
        {
            return false;
        }
    }

    public string GenerateToken(Guid userId, string userName)
    {
        var secretKey = _configuration["Jwt:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException("JWT SecretKey is not configured");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, userName)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "CodeRacer",
            audience: _configuration["Jwt:Audience"] ?? "CodeRacerApp",
            claims: claims,
            expires: DateTime.UtcNow.Add(GetTokenExpiration()),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public TimeSpan GetTokenExpiration()
    {
        var expirationMinutes = int.TryParse(_configuration["Jwt:ExpirationMinutes"], out var minutes)
            ? minutes
            : 60; // Default to 1 hour

        return TimeSpan.FromMinutes(expirationMinutes);
    }
}
