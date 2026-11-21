using CodeRacer.Server.Dtos;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Interfaces;

public interface ILeaderboardService
{
    Task<IReadOnlyList<LeaderboardEntryDto>> GetAsync(
        LeaderboardQuery query,
        CancellationToken cancellationToken);
}
