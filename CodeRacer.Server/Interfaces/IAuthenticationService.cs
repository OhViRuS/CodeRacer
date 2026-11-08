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

    /// <summary>Generates a JWT token for the user. Token expires in the configured duration.</summary>
    string GenerateToken(Guid userId, string userName);

    /// <summary>Gets the configured token expiration duration.</summary>
    TimeSpan GetTokenExpiration();
}
