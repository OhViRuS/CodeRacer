using CodeRacer.Server.Dtos;
using CodeRacer.Server.Interfaces;

namespace CodeRacer.Server.Services;

/// <summary>
/// Builds a user's statistics. The figures are aggregated by the database, so the cost
/// does not grow with the number of races (the shared guest account can have very many).
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

        var statistics = await _raceResultRepository.GetStatisticsByUserIdAsync(userId, cancellationToken);
        if (statistics == null)
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
            statistics.TotalRaces,
            statistics.AverageTypingSpeed,
            statistics.AverageAccuracy,
            statistics.BestTypingSpeed,
            statistics.BestCompletionTime,
            statistics.TotalMistakes,
            statistics.FirstRaceDate,
            statistics.LastRaceDate
        );
    }
}
