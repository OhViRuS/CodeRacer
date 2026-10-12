using System;

namespace CodeRacer.Server.Models
{
    public record RaceResult(TimeSpan CompletionTime, double TypingSpeed, double Accuracy) : IComparable<RaceResult>
    {
        public int CompareTo(RaceResult? other)
        {
            if (other == null) return 1;

            var cmp = other.TypingSpeed.CompareTo(this.TypingSpeed);
            if (cmp != 0) return cmp;

            return this.CompletionTime.CompareTo(other.CompletionTime);
        }
    }
}