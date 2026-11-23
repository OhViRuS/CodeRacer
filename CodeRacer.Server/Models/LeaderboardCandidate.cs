namespace CodeRacer.Server.Models;

public sealed record LeaderboardCandidate(
    Guid UserId,
    string UserName,
    Guid RaceResultId,
    double TypingSpeed,
    double Accuracy,
    TimeSpan CompletionTime,
    int Mistakes,
    DateTime CreatedAt);
