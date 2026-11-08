using CodeRacer.Server.Dtos;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Services;

/// <summary>
/// Race result service implementation.
/// Coordinates race result persistence and retrieval.
/// Uses IRaceResultRepository for data access, and makes sure the raced snippet
/// (loaded from snippets.json) also exists in the database before a result references it.
/// Every method checks the cancellation token first, so a cancelled request does no work.
/// </summary>
public class RaceResultService(
    IRaceResultRepository raceResultRepository,
    ICodeSnippetProvider snippetProvider,
    ISnippetRecordRepository snippetRecordRepository,
    TimeProvider timeProvider) : IRaceResultService
{
    private readonly IRaceResultRepository _raceResultRepository = raceResultRepository;
    private readonly ICodeSnippetProvider _snippetProvider = snippetProvider;
    private readonly ISnippetRecordRepository _snippetRecordRepository = snippetRecordRepository;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<RaceResultDto?> SaveRaceResultAsync(
        Guid userId,
        SaveRaceResultRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var snippet = _snippetProvider.GetById(request.CodeSnippetId);
        if (snippet == null)
        {
            return null;
        }

        await _snippetRecordRepository.EnsureExistsAsync(snippet, cancellationToken);

        var raceResult = new RaceResult(
            completionTime: request.CompletionTime,
            typingSpeed: request.TypingSpeed,
            accuracy: request.Accuracy)
        {
            UserId = userId,
            CodeSnippetId = request.CodeSnippetId,
            Mistakes = request.Mistakes,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        var savedResult = await _raceResultRepository.AddAsync(raceResult, cancellationToken);

        return MapToDto(savedResult);
    }

    public async Task<RaceResultDto?> GetRaceResultByIdAsync(Guid resultId, Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

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
        cancellationToken.ThrowIfCancellationRequested();

        var results = await _raceResultRepository.GetByUserIdAsync(userId, cancellationToken);
        return results.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<RaceResultDto>> GetRecentRaceResultsAsync(
        Guid userId,
        int count,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var results = await _raceResultRepository.GetRecentByUserIdAsync(userId, count, cancellationToken);
        return results.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<RaceResultDto>> GetRaceResultsByDateRangeAsync(
        Guid userId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var results = await _raceResultRepository.GetByUserIdAndDateRangeAsync(userId, from, to, cancellationToken);
        return results.Select(MapToDto).ToList();
    }

    public async Task<bool> DeleteRaceResultAsync(Guid resultId, Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

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
