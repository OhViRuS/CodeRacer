using CodeRacer.Server.Dtos;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Interfaces;

/// <summary>
/// Short-lived in-memory cache of leaderboards, shared by all requests.
/// </summary>
public interface ILeaderboardCache
{
    /// <summary>
    /// Returns the cached leaderboard for <paramref name="query"/> if it is still fresh; otherwise
    /// runs <paramref name="load"/>, caches its result and returns it. Concurrent callers asking for
    /// the same query while it is being loaded wait for that load instead of starting their own.
    /// </summary>
    Task<IReadOnlyList<LeaderboardEntryDto>> GetOrLoadAsync(
        LeaderboardQuery query,
        Func<CancellationToken, Task<IReadOnlyList<LeaderboardEntryDto>>> load,
        CancellationToken cancellationToken);
}
