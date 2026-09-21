using System;

namespace CodeRacer.Server.Models
{
    public record RaceResult(TimeSpan CompletionTime, double TypingSpeed, double Accuracy) : IComparable<RaceResult>
    {
        public int CompareTo(RaceResult? other)
        {
            if (other == null) return 1;

            return other.TypingSpeed.CompareTo(this.TypingSpeed);
        }
    }
}