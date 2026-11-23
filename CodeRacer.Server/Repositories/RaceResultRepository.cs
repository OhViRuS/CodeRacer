using CodeRacer.Server.Data;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using CodeRacer.Server.Strategies;
using Microsoft.EntityFrameworkCore;

namespace CodeRacer.Server.Repositories;

/// <summary>
/// RaceResult repository implementation.
/// Provides race-result-specific queries for history, statistics, and leaderboards,
/// built on the generic FindOrderedDescendingAsync from <see cref="Repository{T}"/>.
/// </summary>
public class RaceResultRepository(
    CodeRacerDbContext context,
    IEnumerable<ILeaderboardSortStrategy>? leaderboardSortStrategies = null) : Repository<RaceResult>(context), IRaceResultRepository
{
    private readonly IReadOnlyDictionary<LeaderboardSortBy, ILeaderboardSortStrategy> _leaderboardSortStrategies =
        (leaderboardSortStrategies?.Any() == true
            ? leaderboardSortStrategies
            : DefaultLeaderboardSortStrategies()).ToDictionary(strategy => strategy.SortBy);

    public Task<IReadOnlyList<RaceResult>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        FindOrderedDescendingAsync(
            r => r.UserId == userId,
            r => r.CreatedAt,
            take: null,
            cancellationToken);

    public Task<IReadOnlyList<RaceResult>> GetByUserIdAndDateRangeAsync(
        Guid userId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken) =>
        FindOrderedDescendingAsync(
            r => r.UserId == userId && r.CreatedAt >= from && r.CreatedAt <= to,
            r => r.CreatedAt,
            take: null,
            cancellationToken);

    public Task<IReadOnlyList<RaceResult>> GetRecentByUserIdAsync(
        Guid userId,
        int count,
        CancellationToken cancellationToken) =>
        FindOrderedDescendingAsync(
            r => r.UserId == userId,
            r => r.CreatedAt,
            take: count,
            cancellationToken);

    public async Task<RaceStatistics?> GetStatisticsByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await DbSet
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .GroupBy(r => r.UserId)
            .Select(g => new RaceStatistics(
                g.Count(),
                g.Average(r => r.TypingSpeed),
                g.Average(r => r.Accuracy),
                g.Max(r => r.TypingSpeed),
                g.Min(r => r.CompletionTime),
                g.Sum(r => (long)r.Mistakes), // 64-bit: many capped races must still not overflow
                g.Min(r => r.CreatedAt),
                g.Max(r => r.CreatedAt)))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeaderboardRow>> GetLeaderboardAsync(
        LeaderboardQuery query,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedTopic = query.Topic?.Trim().ToLowerInvariant();
        var filteredResults = DbSet
            .AsNoTracking()
            .Where(race => race.UserId != GuestAccount.Id)
            .Where(race => Context.CodeSnippets.Any(snippet =>
                snippet.Id == race.CodeSnippetId
                && (query.SnippetId == null || snippet.Id == query.SnippetId)
                && snippet.Language == query.Language
                && snippet.Difficulty == query.Difficulty
                && (normalizedTopic == null || snippet.ProgrammingConcept.ToLower() == normalizedTopic)));

        var strategy = GetStrategy(query.SortBy);
        var bestResults = strategy.SelectBest(filteredResults);

        var orderedResults =
            from race in bestResults
            join user in Context.Users.AsNoTracking() on race.UserId equals user.Id
            select new LeaderboardCandidate(
                race.UserId,
                user.UserName,
                race.Id,
                race.TypingSpeed,
                race.Accuracy,
                race.CompletionTime,
                race.Mistakes,
                race.CreatedAt);

        var orderedQuery = strategy.Order(orderedResults);

        return await orderedQuery
            .Take(query.Limit)
            .Select(result => new LeaderboardRow(
                result.UserId,
                result.UserName,
                result.RaceResultId,
                result.TypingSpeed,
                result.Accuracy,
                result.CompletionTime,
                result.Mistakes,
                result.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private ILeaderboardSortStrategy GetStrategy(LeaderboardSortBy sortBy) =>
        _leaderboardSortStrategies.TryGetValue(sortBy, out var strategy)
            ? strategy
            : throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, "Unsupported leaderboard sort order.");

    private static IEnumerable<ILeaderboardSortStrategy> DefaultLeaderboardSortStrategies() =>
    [
        new TypingSpeedLeaderboardSortStrategy(),
        new AccuracyLeaderboardSortStrategy(),
        new CompletionTimeLeaderboardSortStrategy()
    ];
}
