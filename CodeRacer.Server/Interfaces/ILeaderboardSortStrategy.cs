using CodeRacer.Server.Models;

namespace CodeRacer.Server.Interfaces;

public interface ILeaderboardSortStrategy
{
    LeaderboardSortBy SortBy { get; }

    IQueryable<RaceResult> SelectBest(IQueryable<RaceResult> candidates);

    IOrderedQueryable<LeaderboardCandidate> Order(IQueryable<LeaderboardCandidate> candidates);
}
