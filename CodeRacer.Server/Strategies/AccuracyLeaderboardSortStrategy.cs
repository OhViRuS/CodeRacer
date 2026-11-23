using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Strategies;

public sealed class AccuracyLeaderboardSortStrategy : ILeaderboardSortStrategy
{
    public LeaderboardSortBy SortBy => LeaderboardSortBy.Accuracy;

    public IQueryable<RaceResult> SelectBest(IQueryable<RaceResult> candidates) =>
        candidates.Where(result => !candidates.Any(other =>
            other.UserId == result.UserId
            && (other.Accuracy > result.Accuracy
                || other.Accuracy == result.Accuracy && other.TypingSpeed > result.TypingSpeed
                || other.Accuracy == result.Accuracy && other.TypingSpeed == result.TypingSpeed && other.CompletionTime < result.CompletionTime
                || other.Accuracy == result.Accuracy && other.TypingSpeed == result.TypingSpeed && other.CompletionTime == result.CompletionTime && other.CreatedAt < result.CreatedAt)));

    public IOrderedQueryable<LeaderboardCandidate> Order(IQueryable<LeaderboardCandidate> candidates) =>
        candidates
            .OrderByDescending(result => result.Accuracy)
            .ThenByDescending(result => result.TypingSpeed)
            .ThenBy(result => result.CompletionTime)
            .ThenBy(result => result.CreatedAt)
            .ThenBy(result => result.UserName);
}
