using CodeRacer.Server.Dtos;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Services;

/// <summary>
/// Race result service implementation.
/// Coordinates race result persistence and retrieval.
/// Uses IRaceResultRepository for data access and checks that the raced snippet exists before saving a result.
/// Results of players who are not signed in are saved under the shared <see cref="GuestAccount"/>.
/// Every method checks the cancellation token first, so a cancelled request does no work.
/// </summary>
public class RaceResultService(
    IRaceResultRepository raceResultRepository,
    ICodeSnippetProvider snippetProvider,
    IUserRepository userRepository,
    TimeProvider timeProvider) : IRaceResultService
{
    private readonly IRaceResultRepository _raceResultRepository = raceResultRepository;
    private readonly ICodeSnippetProvider _snippetProvider = snippetProvider;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<RaceResultDto?> SaveRaceResultAsync(
        Guid? userId,
        SaveRaceResultRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var snippet = await _snippetProvider.GetByIdAsync(request.CodeSnippetId, cancellationToken);
        if (snippet == null)
        {
            return null;
        }

        if (userId == null)
        {
            await _userRepository.EnsureGuestUserAsync(cancellationToken);
        }

        var raceResult = new RaceResult(
            completionTime: request.CompletionTime,
            typingSpeed: request.TypingSpeed,
            accuracy: request.Accuracy)
        {
            UserId = userId ?? GuestAccount.Id,
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
