using System.ComponentModel.DataAnnotations;

namespace CodeRacer.Server.Dtos;

/// <summary>
/// DTO for user registration request.
/// Separates the HTTP contract from domain models (following SOLID).
/// The attributes give clients field-level 400 errors; UserService repeats the checks.
/// </summary>
public record RegisterUserRequest(
    [Required, StringLength(50, MinimumLength = 1), RegularExpression("^[^@]+$", ErrorMessage = "Username cannot contain '@'.")] string UserName,
    [Required, EmailAddress, StringLength(320)] string Email,
    [Required, MinLength(8)] string Password,
    [Required] string ConfirmPassword
);

/// <summary>
/// DTO for user login request.
/// </summary>
public record LoginRequest(
    [Required] string UserNameOrEmail,
    [Required] string Password
);

/// <summary>
/// DTO for the signed-in user's own data (login response and /api/users/me).
/// Does not include password hash for security.
/// </summary>
public record UserDto(
    Guid Id,
    string UserName,
    string Email,
    DateTime CreatedAt
);

/// <summary>
/// DTO for another user's public profile.
/// Leaves out the email address, which only the user themself may see.
/// </summary>
public record PublicUserDto(
    Guid Id,
    string UserName,
    DateTime CreatedAt
);

/// <summary>
/// DTO for authentication response.
/// Includes the JWT token and user information.
/// </summary>
public record AuthenticationResponse(
    string Token,
    UserDto User,
    DateTime ExpiresAt
);

/// <summary>
/// DTO for race result in responses.
/// Includes user identifiable information.
/// </summary>
public record RaceResultDto(
    Guid Id,
    Guid UserId,
    Guid CodeSnippetId,
    TimeSpan CompletionTime,
    double TypingSpeed,
    double Accuracy,
    int Mistakes,
    DateTime CreatedAt
);

/// <summary>
/// DTO for saving a race result.
/// Sent from the client after completing a typing challenge.
/// </summary>
public record SaveRaceResultRequest(
    Guid CodeSnippetId,
    [Range(typeof(TimeSpan), "00:00:00.001", "1.00:00:00", ErrorMessage = "Completion time must be positive and less than a day.")] TimeSpan CompletionTime,
    [Range(0, double.MaxValue)] double TypingSpeed,
    [Range(0, 100)] double Accuracy,
    [Range(0, int.MaxValue)] int Mistakes
);

/// <summary>
/// DTO for user statistics.
/// BestCompletionTime, FirstRaceDate and LastRaceDate are null until the user has raced.
/// </summary>
public record UserStatisticsDto(
    Guid UserId,
    int TotalRaces,
    double AverageTypingSpeed,
    double AverageAccuracy,
    double BestTypingSpeed,
    TimeSpan? BestCompletionTime,
    int TotalMistakes,
    DateTime? FirstRaceDate,
    DateTime? LastRaceDate
);
