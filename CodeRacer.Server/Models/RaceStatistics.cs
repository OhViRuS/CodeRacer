namespace CodeRacer.Server.Models;

/// <summary>
/// Aggregated race figures for one user, calculated by the database in a single query.
/// </summary>
public record RaceStatistics(
    int TotalRaces,
    double AverageTypingSpeed,
    double AverageAccuracy,
    double BestTypingSpeed,
    TimeSpan BestCompletionTime,
    int TotalMistakes,
    DateTime FirstRaceDate,
    DateTime LastRaceDate
);
