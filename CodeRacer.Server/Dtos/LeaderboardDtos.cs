namespace CodeRacer.Server.Dtos;

public record LeaderboardEntryDto(
    int Rank,
    Guid UserId,
    string UserName,
    Guid RaceResultId,
    double TypingSpeed,
    double Accuracy,
    TimeSpan CompletionTime,
    int Mistakes,
    DateTime CreatedAt);
