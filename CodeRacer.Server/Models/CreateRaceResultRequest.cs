using System;
using System.ComponentModel.DataAnnotations;

namespace CodeRacer.Server.Models;

public class CreateRaceResultRequest
{
    public Guid? UserId { get; set; }

    public Guid? CodeSnippetId { get; set; }

    [Required]
    public TimeSpan CompletionTime { get; set; }

    [Range(0, 500)]
    public double TypingSpeed { get; set; }

    [Range(0, 100)]
    public double Accuracy { get; set; }

    [Range(0, 10000)]
    public int Mistakes { get; set; }
}
