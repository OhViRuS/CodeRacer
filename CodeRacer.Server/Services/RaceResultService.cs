using CodeRacer.Server.Dtos;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Services;

/// <summary>
/// Race result service implementation.
/// Coordinates race result persistence and retrieval.
/// Uses IRaceResultRepository for data access.
/// </summary>
public class RaceResultService(IRaceResultRepository raceResultRepository) : IRaceResultService
{
    private readonly IRaceResultRepository _raceResultRepository = raceResultRepository;

    public async Task<RaceResultDto> SaveRaceResultAsync(
        Guid userId,
        SaveRaceResultRequest request,
        CancellationToken cancellationToken)
    {
        var raceResult = new RaceResult(
            completionTime: request.CompletionTime,
            typingSpeed: request.TypingSpeed,
            accuracy: request.Accuracy)
        {
            UserId = userId,
            CodeSnippetId = request.CodeSnippetId,
            Mistakes = request.Mistakes,
            CreatedAt = DateTime.UtcNow
        };

        var savedResult = await _raceResultRepository.AddAsync(raceResult, cancellationToken);

        return MapToDto(savedResult);
    }

    public async Task<RaceResultDto?> GetRaceResultByIdAsync(Guid resultId, Guid userId, CancellationToken cancellationToken)
    {
        var result = await _raceResultRepository.GetByIdAsync(resultId, cancellationToken);

        // Ensure user can only access their own results
        if (result == null || result.UserId != userId)
        {
            return null;
        }

        return MapToDto(result);
    }

    public async Task<IReadOnlyList<RaceResultDto>> GetUserRaceResultsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var results = await _raceResultRepository.GetByUserIdAsync(userId, cancellationToken);
        return results.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<RaceResultDto>> GetRecentRaceResultsAsync(
        Guid userId,
        int count,
        CancellationToken cancellationToken)
    {
        var results = await _raceResultRepository.GetRecentByUserIdAsync(userId, count, cancellationToken);
        return results.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<RaceResultDto>> GetRaceResultsByDateRangeAsync(
        Guid userId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        var results = await _raceResultRepository.GetByUserIdAndDateRangeAsync(userId, from, to, cancellationToken);
        return results.Select(MapToDto).ToList();
    }

    public async Task<bool> DeleteRaceResultAsync(Guid resultId, Guid userId, CancellationToken cancellationToken)
    {
        var result = await _raceResultRepository.GetByIdAsync(resultId, cancellationToken);

        // Ensure user can only delete their own results
        if (result == null || result.UserId != userId)
        {
            return false;
        }

        return await _raceResultRepository.DeleteAsync(resultId, cancellationToken);
    }

    private static RaceResultDto MapToDto(RaceResult result) =>
        new(
            result.Id,
            result.UserId,
            result.CodeSnippetId,
            result.CompletionTime,
            result.TypingSpeed,
            result.Accuracy,
            result.Mistakes,
            result.CreatedAt
        );
}
