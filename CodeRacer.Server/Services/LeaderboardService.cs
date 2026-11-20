using CodeRacer.Server.Dtos;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Services;

public class LeaderboardService(IRaceResultRepository raceResultRepository) : ILeaderboardService
{
    public const int MaxLimit = 100;

    private readonly IRaceResultRepository _raceResultRepository = raceResultRepository;

    public async Task<IReadOnlyList<LeaderboardEntryDto>> GetAsync(
        LeaderboardQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        var limit = query.Limit;
        if (limit is < 1 or > MaxLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(query.Limit), $"Limit must be between 1 and {MaxLimit}.");
        }

        var normalizedQuery = query with
        {
            Limit = limit,
            Topic = string.IsNullOrWhiteSpace(query.Topic) ? null : query.Topic.Trim()
        };

        var rows = await _raceResultRepository.GetLeaderboardAsync(normalizedQuery, cancellationToken);
        var orderedRows = OrderRows(rows, normalizedQuery.SortBy);

        return orderedRows
            .Take(limit)
            .Select((row, index) => new LeaderboardEntryDto(
                Rank: index + 1,
                UserId: row.UserId,
                UserName: row.UserName,
                RaceResultId: row.RaceResultId,
                TypingSpeed: row.TypingSpeed,
                Accuracy: row.Accuracy,
                CompletionTime: row.CompletionTime,
                Mistakes: row.Mistakes,
                CreatedAt: row.CreatedAt))
            .ToList();
    }

    private static IEnumerable<LeaderboardRow> OrderRows(
        IEnumerable<LeaderboardRow> rows,
        LeaderboardSortBy sortBy) => sortBy switch
        {
            LeaderboardSortBy.TypingSpeed => rows
                .OrderByDescending(row => row.TypingSpeed)
                .ThenBy(row => row.CompletionTime)
                .ThenByDescending(row => row.Accuracy)
                .ThenBy(row => row.UserName),
            LeaderboardSortBy.Accuracy => rows
                .OrderByDescending(row => row.Accuracy)
                .ThenByDescending(row => row.TypingSpeed)
                .ThenBy(row => row.CompletionTime)
                .ThenBy(row => row.UserName),
            LeaderboardSortBy.CompletionTime => rows
                .OrderBy(row => row.CompletionTime)
                .ThenByDescending(row => row.TypingSpeed)
                .ThenByDescending(row => row.Accuracy)
                .ThenBy(row => row.UserName),
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, "Unsupported leaderboard sort order.")
        };
}
