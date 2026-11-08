using CodeRacer.Server.Dtos;

namespace CodeRacer.Server.Interfaces;

/// <summary>
/// Calculates a user's statistics from their race results.
/// Kept apart from IUserService (Single Responsibility Principle): accounts and statistics change for different reasons.
/// </summary>
public interface IUserStatisticsService
{
    /// <summary>
    /// Gets user statistics (races completed, average speed, etc).
    /// Returns null if user not found.
    /// </summary>
    Task<UserStatisticsDto?> GetUserStatisticsAsync(Guid userId, CancellationToken cancellationToken);
}
