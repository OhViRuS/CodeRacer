using CodeRacer.Server.Dtos;

namespace CodeRacer.Server.Interfaces;

/// <summary>
/// Service for managing race results.
/// Handles saving, retrieving, and querying race results for users.
/// Following Single Responsibility Principle - focuses only on race result business logic.
/// </summary>
public interface IRaceResultService
{
    /// <summary>
    /// Saves a race result for a user. Returns the saved race result DTO,
    /// or null if the referenced code snippet does not exist.
    /// </summary>
    Task<RaceResultDto?> SaveRaceResultAsync(
        Guid userId,
        SaveRaceResultRequest request,
        CancellationToken cancellationToken);

    /// <summary>Gets a single race result by ID. Returns null if not found or user doesn't have access.</summary>
    Task<RaceResultDto?> GetRaceResultByIdAsync(Guid resultId, Guid userId, CancellationToken cancellationToken);

    /// <summary>Gets all race results for a user, ordered by most recent first.</summary>
    Task<IReadOnlyList<RaceResultDto>> GetUserRaceResultsAsync(
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>Gets the most recent N race results for a user.</summary>
    Task<IReadOnlyList<RaceResultDto>> GetRecentRaceResultsAsync(
        Guid userId,
        int count,
        CancellationToken cancellationToken);

    /// <summary>Gets race results for a specific date range for a user.</summary>
    Task<IReadOnlyList<RaceResultDto>> GetRaceResultsByDateRangeAsync(
        Guid userId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken);

    /// <summary>Deletes a race result. Returns false if result not found or user doesn't have access.</summary>
    Task<bool> DeleteRaceResultAsync(Guid resultId, Guid userId, CancellationToken cancellationToken);
}
