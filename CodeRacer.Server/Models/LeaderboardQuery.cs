namespace CodeRacer.Server.Models;

public record LeaderboardQuery(
    LeaderboardSortBy SortBy,
    ProgrammingLanguage Language,
    Difficulty Difficulty,
    string? Topic = null,
    int Limit = 10);
