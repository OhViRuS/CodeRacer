namespace CodeRacer.Server.Models;

public class RaceResult : IComparable<RaceResult>
{
    public RaceResult()
    {
    }

    public RaceResult(TimeSpan completionTime, double typingSpeed, double accuracy)
    {
        CompletionTime = completionTime;
        TypingSpeed = typingSpeed;
        Accuracy = accuracy;
    }

    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid CodeSnippetId { get; set; }
    public TimeSpan CompletionTime { get; set; }
    public double TypingSpeed { get; set; }
    public double Accuracy { get; set; }
    public int Mistakes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public CodeSnippet? CodeSnippet { get; set; }

    public int CompareTo(RaceResult? other)
    {
        if (other == null)
        {
            return 1;
        }

        var cmp = other.TypingSpeed.CompareTo(this.TypingSpeed);
        if (cmp != 0)
        {
            return cmp;
        }

        return this.CompletionTime.CompareTo(other.CompletionTime);
    }
}
