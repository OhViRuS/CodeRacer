using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Strategies;

public sealed class CompletionTimeLeaderboardSortStrategy : ILeaderboardSortStrategy
{
    public LeaderboardSortBy SortBy => LeaderboardSortBy.CompletionTime;

    public IQueryable<RaceResult> SelectBest(IQueryable<RaceResult> candidates) =>
        candidates.Where(result => !candidates.Any(other =>
            other.UserId == result.UserId
            && (other.CompletionTime < result.CompletionTime
                || other.CompletionTime == result.CompletionTime && other.TypingSpeed > result.TypingSpeed
                || other.CompletionTime == result.CompletionTime && other.TypingSpeed == result.TypingSpeed && other.Accuracy > result.Accuracy
                || other.CompletionTime == result.CompletionTime && other.TypingSpeed == result.TypingSpeed && other.Accuracy == result.Accuracy && other.CreatedAt < result.CreatedAt)));

    public IOrderedQueryable<LeaderboardCandidate> Order(IQueryable<LeaderboardCandidate> candidates) =>
        candidates
            .OrderBy(result => result.CompletionTime)
            .ThenByDescending(result => result.TypingSpeed)
            .ThenByDescending(result => result.Accuracy)
            .ThenBy(result => result.CreatedAt)
            .ThenBy(result => result.UserName);
}
