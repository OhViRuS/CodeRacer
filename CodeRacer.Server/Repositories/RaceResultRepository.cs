using CodeRacer.Server.Data;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeRacer.Server.Repositories;

/// <summary>
/// RaceResult repository implementation.
/// Provides race-result-specific queries for history, statistics, and leaderboards.
/// </summary>
public class RaceResultRepository(CodeRacerDbContext context) : Repository<RaceResult>(context), IRaceResultRepository
{
    public async Task<IReadOnlyList<RaceResult>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await DbSet
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RaceResult>> GetByUserIdAndDateRangeAsync(
        Guid userId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await DbSet
            .AsNoTracking()
            .Where(r => r.UserId == userId && r.CreatedAt >= from && r.CreatedAt <= to)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RaceResult>> GetRecentByUserIdAsync(
        Guid userId,
        int count,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await DbSet
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
