namespace CodeRacer.Server.Dtos;

/// <summary>
/// DTO for user registration request.
/// Separates the HTTP contract from domain models (following SOLID).
/// </summary>
public record RegisterUserRequest(
    string UserName,
    string Email,
    string Password,
    string ConfirmPassword
);

/// <summary>
/// DTO for user login request.
/// </summary>
public record LoginRequest(
    string UserNameOrEmail,
    string Password
);

/// <summary>
/// DTO for user data in responses.
/// Does not include password hash for security.
/// </summary>
public record UserDto(
    Guid Id,
    string UserName,
    string Email,
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
    TimeSpan CompletionTime,
    double TypingSpeed,
    double Accuracy,
    int Mistakes
);

/// <summary>
/// DTO for user statistics.
/// </summary>
public record UserStatisticsDto(
    Guid UserId,
    int TotalRaces,
    double AverageTypingSpeed,
    double AverageAccuracy,
    double BestTypingSpeed,
    TimeSpan BestCompletionTime,
    int TotalMistakes,
    DateTime FirstRaceDate,
    DateTime LastRaceDate
);
