using CodeRacer.Server.Dtos;

namespace CodeRacer.Server.Interfaces;

/// <summary>
/// Service for user accounts.
/// Handles registration, login, and profile operations.
/// Following Single Responsibility Principle - statistics live in IUserStatisticsService.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Registers a new user. Validates email/username uniqueness, password confirmation,
    /// and returns authentication response with JWT token.
    /// Returns null if validation fails or the username/email is taken.
    /// </summary>
    Task<AuthenticationResponse?> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Authenticates a user by username or email and password.
    /// Returns authentication response with JWT token if successful, null if credentials are invalid.
    /// </summary>
    Task<AuthenticationResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the signed-in user's own profile, including the email address.
    /// Returns null if user not found.
    /// </summary>
    Task<UserDto?> GetOwnProfileAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets another user's public profile, without the email address.
    /// Returns null if user not found.
    /// </summary>
    Task<PublicUserDto?> GetPublicProfileAsync(Guid userId, CancellationToken cancellationToken);
}
