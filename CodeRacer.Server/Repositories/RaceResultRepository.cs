using CodeRacer.Server.Data;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeRacer.Server.Repositories;

/// <summary>
/// RaceResult repository implementation.
/// Provides race-result-specific queries for history, statistics, and leaderboards,
/// built on the generic FindOrderedDescendingAsync from <see cref="Repository{T}"/>.
/// </summary>
public class RaceResultRepository(CodeRacerDbContext context) : Repository<RaceResult>(context), IRaceResultRepository
{
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

        var filteredResults =
            from race in DbSet.AsNoTracking()
            join user in Context.Users.AsNoTracking() on race.UserId equals user.Id
            join snippet in Context.CodeSnippets.AsNoTracking() on race.CodeSnippetId equals snippet.Id
            where race.UserId != GuestAccount.Id
                && snippet.Language == query.Language
                && snippet.Difficulty == query.Difficulty
                && (query.Topic == null || snippet.ProgrammingConcept == query.Topic)
            select new
            {
                race.UserId,
                user.UserName,
                race.Id,
                race.TypingSpeed,
                race.Accuracy,
                race.CompletionTime,
                race.Mistakes,
                race.CreatedAt
            };

        var matchingResults = await filteredResults.ToListAsync(cancellationToken);

        return matchingResults
            .GroupBy(result => new { result.UserId, result.UserName })
            .Select(group =>
            {
                var bestRace = query.SortBy switch
                {
                    LeaderboardSortBy.TypingSpeed => group
                        .OrderByDescending(result => result.TypingSpeed)
                        .ThenBy(result => result.CompletionTime)
                        .ThenByDescending(result => result.Accuracy)
                        .ThenByDescending(result => result.CreatedAt)
                        .ThenBy(result => result.Id)
                        .First(),
                    LeaderboardSortBy.Accuracy => group
                        .OrderByDescending(result => result.Accuracy)
                        .ThenByDescending(result => result.TypingSpeed)
                        .ThenBy(result => result.CompletionTime)
                        .ThenByDescending(result => result.CreatedAt)
                        .ThenBy(result => result.Id)
                        .First(),
                    LeaderboardSortBy.CompletionTime => group
                        .OrderBy(result => result.CompletionTime)
                        .ThenByDescending(result => result.TypingSpeed)
                        .ThenByDescending(result => result.Accuracy)
                        .ThenByDescending(result => result.CreatedAt)
                        .ThenBy(result => result.Id)
                        .First(),
                    _ => throw new ArgumentOutOfRangeException(nameof(query.SortBy), query.SortBy, "Unsupported leaderboard sort order.")
                };

                return new LeaderboardRow(
                    group.Key.UserId,
                    group.Key.UserName,
                    bestRace.Id,
                    bestRace.TypingSpeed,
                    bestRace.Accuracy,
                    bestRace.CompletionTime,
                    bestRace.Mistakes,
                    bestRace.CreatedAt);
            })
            .ToList();
    }
}
