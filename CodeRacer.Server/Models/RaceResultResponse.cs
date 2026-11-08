using System;

namespace CodeRacer.Server.Models;

public class RaceResultResponse
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public Guid? CodeSnippetId { get; set; }

    public TimeSpan CompletionTime { get; set; }

    public double TypingSpeed { get; set; }

    public double Accuracy { get; set; }

    public int Mistakes { get; set; }

    public DateTime CreatedAt { get; set; }

    public static RaceResultResponse FromEntity(RaceResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new RaceResultResponse
        {
            Id = result.Id,
            UserId = result.UserId,
            CodeSnippetId = result.CodeSnippetId,
            CompletionTime = result.CompletionTime,
            TypingSpeed = result.TypingSpeed,
            Accuracy = result.Accuracy,
            Mistakes = result.Mistakes,
            CreatedAt = result.CreatedAt
        };
    }
}
