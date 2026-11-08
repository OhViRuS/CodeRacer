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
}
