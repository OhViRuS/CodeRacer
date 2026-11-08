using CodeRacer.Server.Dtos;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Interfaces;

/// <summary>
/// Service for user management.
/// Handles registration, login, and profile operations.
/// Following Single Responsibility Principle - focuses only on user business logic.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Registers a new user. Validates email/username uniqueness, password confirmation,
    /// and returns authentication response with JWT token.
    /// Returns null if validation fails.
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
    /// Gets a user's profile information by user ID.
    /// Returns null if user not found.
    /// </summary>
    Task<UserDto?> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets user statistics (races completed, average speed, etc).
    /// Returns null if user not found.
    /// </summary>
    Task<UserStatisticsDto?> GetUserStatisticsAsync(Guid userId, CancellationToken cancellationToken);
}
