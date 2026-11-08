using CodeRacer.Server.Models;

namespace CodeRacer.Server.Interfaces;

/// <summary>
/// Service for authentication and token generation.
/// Handles JWT token creation and password hashing/verification.
/// Following Interface Segregation Principle - separate concerns.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>Hashes a password using bcrypt.</summary>
    string HashPassword(string password);

    /// <summary>Verifies a password against its hash. Returns true if password matches.</summary>
    bool VerifyPassword(string password, string hash);

    /// <summary>
    /// Generates a signed JWT for the user, valid for the configured duration.
    /// Returns the token together with its expiry time.
    /// </summary>
    AuthenticationToken GenerateToken(Guid userId, string userName);
}
