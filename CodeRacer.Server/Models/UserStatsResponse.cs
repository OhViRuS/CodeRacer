using System;

namespace CodeRacer.Server.Models;

public class UserStatsResponse
{
    public Guid UserId { get; set; }

    public int TotalRaces { get; set; }

    public double BestTypingSpeed { get; set; }

    public double AverageTypingSpeed { get; set; }

    public double AverageAccuracy { get; set; }

    public int TotalMistakes { get; set; }

    public TimeSpan TotalTimeSpent { get; set; }
}
