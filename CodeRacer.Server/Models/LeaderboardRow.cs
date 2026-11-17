namespace CodeRacer.Server.Models;

public record LeaderboardRow(
    Guid UserId,
    string UserName,
    Guid RaceResultId,
    double TypingSpeed,
    double Accuracy,
    TimeSpan CompletionTime,
    int Mistakes,
    DateTime CreatedAt);
