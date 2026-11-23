namespace CodeRacer.Server.Models;

public sealed class LeaderboardCandidate
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public Guid RaceResultId { get; set; }
    public double TypingSpeed { get; set; }
    public double Accuracy { get; set; }
    public TimeSpan CompletionTime { get; set; }
    public int Mistakes { get; set; }
    public DateTime CreatedAt { get; set; }
}
