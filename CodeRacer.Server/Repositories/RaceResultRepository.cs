using CodeRacer.Server.Data;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;

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
}
