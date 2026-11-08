using CodeRacer.Server.Dtos;
using CodeRacer.Server.Interfaces;

namespace CodeRacer.Server.Services;

/// <summary>
/// Calculates statistics over a user's race results with LINQ to Objects.
/// </summary>
public class UserStatisticsService(
    IUserRepository userRepository,
    IRaceResultRepository raceResultRepository) : IUserStatisticsService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IRaceResultRepository _raceResultRepository = raceResultRepository;

    public async Task<UserStatisticsDto?> GetUserStatisticsAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return null;
        }

        var raceResults = await _raceResultRepository.GetByUserIdAsync(userId, cancellationToken);
        if (raceResults.Count == 0)
        {
            return new UserStatisticsDto(
                userId,
                TotalRaces: 0,
                AverageTypingSpeed: 0.0,
                AverageAccuracy: 0.0,
                BestTypingSpeed: 0.0,
                BestCompletionTime: null,
                TotalMistakes: 0,
                FirstRaceDate: null,
                LastRaceDate: null
            );
        }

        return new UserStatisticsDto(
            userId,
            TotalRaces: raceResults.Count,
            AverageTypingSpeed: raceResults.Average(r => r.TypingSpeed),
            AverageAccuracy: raceResults.Average(r => r.Accuracy),
            BestTypingSpeed: raceResults.Max(r => r.TypingSpeed),
            BestCompletionTime: raceResults.Min(r => r.CompletionTime),
            TotalMistakes: raceResults.Sum(r => r.Mistakes),
            FirstRaceDate: raceResults.Min(r => r.CreatedAt),
            LastRaceDate: raceResults.Max(r => r.CreatedAt)
        );
    }
}
