using CodeRacer.Server.Dtos;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Services;

/// <summary>
/// Builds ranked leaderboards. Results are served through the shared <see cref="ILeaderboardCache"/>,
/// so a new race result can take up to <see cref="LeaderboardCache.TimeToLive"/> to appear.
/// </summary>
public class LeaderboardService(
    IRaceResultRepository raceResultRepository,
    ILeaderboardCache leaderboardCache) : ILeaderboardService
{
    public const int MaxLimit = 100;

    private readonly IRaceResultRepository _raceResultRepository = raceResultRepository;
    private readonly ILeaderboardCache _leaderboardCache = leaderboardCache;

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
            // Lowercased because the repository matches topics case-insensitively.
            Topic = string.IsNullOrWhiteSpace(query.Topic) ? null : query.Topic.Trim().ToLowerInvariant()
        };

        // The normalized query is the cache key, so "Loops", "loops" and "  Loops  " share one entry.
        return await _leaderboardCache.GetOrLoadAsync(
            normalizedQuery,
            token => LoadAsync(normalizedQuery, token),
            cancellationToken);
    }

    private async Task<IReadOnlyList<LeaderboardEntryDto>> LoadAsync(
        LeaderboardQuery query,
        CancellationToken cancellationToken)
    {
        var rows = await _raceResultRepository.GetLeaderboardAsync(query, cancellationToken);
        var orderedRows = OrderRows(rows, query.SortBy);

        return orderedRows
            .Take(query.Limit)
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
                .ThenBy(row => row.CreatedAt)
                .ThenBy(row => row.UserName),
            LeaderboardSortBy.Accuracy => rows
                .OrderByDescending(row => row.Accuracy)
                .ThenByDescending(row => row.TypingSpeed)
                .ThenBy(row => row.CompletionTime)
                .ThenBy(row => row.CreatedAt)
                .ThenBy(row => row.UserName),
            LeaderboardSortBy.CompletionTime => rows
                .OrderBy(row => row.CompletionTime)
                .ThenByDescending(row => row.TypingSpeed)
                .ThenByDescending(row => row.Accuracy)
                .ThenBy(row => row.CreatedAt)
                .ThenBy(row => row.UserName),
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, "Unsupported leaderboard sort order.")
        };
}
