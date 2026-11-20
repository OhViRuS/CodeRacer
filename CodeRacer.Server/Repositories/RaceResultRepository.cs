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

        var normalizedTopic = query.Topic?.Trim().ToLowerInvariant();
        var filteredResults = DbSet
            .AsNoTracking()
            .Where(race => race.UserId != GuestAccount.Id)
            .Where(race => Context.CodeSnippets.Any(snippet =>
                snippet.Id == race.CodeSnippetId
                && snippet.Language == query.Language
                && snippet.Difficulty == query.Difficulty
                && (normalizedTopic == null || snippet.ProgrammingConcept.ToLower() == normalizedTopic)));

        var bestResults = query.SortBy switch
        {
            LeaderboardSortBy.TypingSpeed => filteredResults.Where(result => !filteredResults.Any(other =>
                other.UserId == result.UserId
                && (other.TypingSpeed > result.TypingSpeed
                    || other.TypingSpeed == result.TypingSpeed && other.CompletionTime < result.CompletionTime
                    || other.TypingSpeed == result.TypingSpeed && other.CompletionTime == result.CompletionTime && other.Accuracy > result.Accuracy
                    || other.TypingSpeed == result.TypingSpeed && other.CompletionTime == result.CompletionTime && other.Accuracy == result.Accuracy && other.CreatedAt > result.CreatedAt))),
            LeaderboardSortBy.Accuracy => filteredResults.Where(result => !filteredResults.Any(other =>
                other.UserId == result.UserId
                && (other.Accuracy > result.Accuracy
                    || other.Accuracy == result.Accuracy && other.TypingSpeed > result.TypingSpeed
                    || other.Accuracy == result.Accuracy && other.TypingSpeed == result.TypingSpeed && other.CompletionTime < result.CompletionTime
                    || other.Accuracy == result.Accuracy && other.TypingSpeed == result.TypingSpeed && other.CompletionTime == result.CompletionTime && other.CreatedAt > result.CreatedAt))),
            LeaderboardSortBy.CompletionTime => filteredResults.Where(result => !filteredResults.Any(other =>
                other.UserId == result.UserId
                && (other.CompletionTime < result.CompletionTime
                    || other.CompletionTime == result.CompletionTime && other.TypingSpeed > result.TypingSpeed
                    || other.CompletionTime == result.CompletionTime && other.TypingSpeed == result.TypingSpeed && other.Accuracy > result.Accuracy
                    || other.CompletionTime == result.CompletionTime && other.TypingSpeed == result.TypingSpeed && other.Accuracy == result.Accuracy && other.CreatedAt > result.CreatedAt))),
            _ => throw new ArgumentOutOfRangeException(nameof(query.SortBy), query.SortBy, "Unsupported leaderboard sort order.")
        };

        var orderedResults =
            from race in bestResults
            join user in Context.Users.AsNoTracking() on race.UserId equals user.Id
            select new { race, user.UserName };

        var orderedQuery = query.SortBy switch
        {
            LeaderboardSortBy.TypingSpeed => orderedResults
                .OrderByDescending(result => result.race.TypingSpeed)
                .ThenBy(result => result.race.CompletionTime)
                .ThenByDescending(result => result.race.Accuracy)
                .ThenBy(result => result.UserName),
            LeaderboardSortBy.Accuracy => orderedResults
                .OrderByDescending(result => result.race.Accuracy)
                .ThenByDescending(result => result.race.TypingSpeed)
                .ThenBy(result => result.race.CompletionTime)
                .ThenBy(result => result.UserName),
            LeaderboardSortBy.CompletionTime => orderedResults
                .OrderBy(result => result.race.CompletionTime)
                .ThenByDescending(result => result.race.TypingSpeed)
                .ThenByDescending(result => result.race.Accuracy)
                .ThenBy(result => result.UserName),
            _ => throw new ArgumentOutOfRangeException(nameof(query.SortBy), query.SortBy, "Unsupported leaderboard sort order.")
        };

        return await orderedQuery
            .Take(query.Limit)
            .Select(result => new LeaderboardRow(
                result.race.UserId,
                result.UserName,
                result.race.Id,
                result.race.TypingSpeed,
                result.race.Accuracy,
                result.race.CompletionTime,
                result.race.Mistakes,
                result.race.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
