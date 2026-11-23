using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Strategies;

public sealed class TypingSpeedLeaderboardSortStrategy : ILeaderboardSortStrategy
{
    public LeaderboardSortBy SortBy => LeaderboardSortBy.TypingSpeed;

    public IQueryable<RaceResult> SelectBest(IQueryable<RaceResult> candidates) =>
        candidates.Where(result => !candidates.Any(other =>
            other.UserId == result.UserId
            && (other.TypingSpeed > result.TypingSpeed
                || other.TypingSpeed == result.TypingSpeed && other.CompletionTime < result.CompletionTime
                || other.TypingSpeed == result.TypingSpeed && other.CompletionTime == result.CompletionTime && other.Accuracy > result.Accuracy
                || other.TypingSpeed == result.TypingSpeed && other.CompletionTime == result.CompletionTime && other.Accuracy == result.Accuracy && other.CreatedAt < result.CreatedAt)));

    public IOrderedQueryable<LeaderboardCandidate> Order(IQueryable<LeaderboardCandidate> candidates) =>
        candidates
            .OrderByDescending(result => result.TypingSpeed)
            .ThenBy(result => result.CompletionTime)
            .ThenByDescending(result => result.Accuracy)
            .ThenBy(result => result.CreatedAt)
            .ThenBy(result => result.UserName);
}
