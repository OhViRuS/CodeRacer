using CodeRacer.Server.Models;

namespace CodeRacer.Server.Interfaces;

/// <summary>
/// Repository for RaceResult entity.
/// Extends IRepository with race-result-specific queries needed for statistics and history.
/// Following Single Responsibility Principle - each repository handles one aggregate root.
/// </summary>
public interface IRaceResultRepository : IRepository<RaceResult>
{
    /// <summary>Gets all race results for a specific user, ordered by most recent first.</summary>
    Task<IReadOnlyList<RaceResult>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Gets race results for a specific user within a date range.</summary>
    Task<IReadOnlyList<RaceResult>> GetByUserIdAndDateRangeAsync(
        Guid userId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken);

    /// <summary>Gets the most recent N race results for a user.</summary>
    Task<IReadOnlyList<RaceResult>> GetRecentByUserIdAsync(
        Guid userId,
        int count,
        CancellationToken cancellationToken);

    /// <summary>
    /// Calculates a user's race statistics in the database without loading their results.
    /// Returns null when the user has no races.
    /// </summary>
    Task<RaceStatistics?> GetStatisticsByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}
