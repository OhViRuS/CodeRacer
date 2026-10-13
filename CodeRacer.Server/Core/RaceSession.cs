using CodeRacer.Server.Models;

namespace CodeRacer.Server.Core;

public class RaceSession
{
    private readonly List<CharacterMistake> _mistakes = new();
    private int _totalErrorsCount = 0;
    private readonly bool _enableDetailedTracking;
    private readonly int _maxMistakesToTrack;

    public RaceSession(CodeSnippet snippet, bool enableDetailedTracking = true, int maxMistakesToTrack = 500)
    {
        if (snippet == null || string.IsNullOrWhiteSpace(snippet.CodeText))
        {
            throw new ArgumentException("The text cannot be empty");
        }

        if (maxMistakesToTrack < 0)
        {
            throw new ArgumentException("maxMistakesToTrack cannot be negative", nameof(maxMistakesToTrack));
        }

        Snippet = snippet;
        _enableDetailedTracking = enableDetailedTracking;
        _maxMistakesToTrack = maxMistakesToTrack;
    }

    public CodeSnippet Snippet { get; }
    public int CurrentPosition { get; private set; }
    public int TotalKeystrokes { get; private set; }
    public int TotalErrorsCount => _totalErrorsCount;
    public IReadOnlyList<CharacterMistake> Mistakes => _mistakes;
    public bool IsCompleted => CurrentPosition >= Snippet.CodeText.Length;

    public bool TypeCharacter(char typed, int position)
    {
        if (position < 0 || position >= Snippet.CodeText.Length)
        {
            return false;
        }

        TotalKeystrokes++;

        char expected = Snippet.CodeText[position];

        if (typed == expected)
        {
            if (position == CurrentPosition)
            {
                CurrentPosition++;
            }

            return true;
        }

        _totalErrorsCount++;

        if (_enableDetailedTracking && _mistakes.Count < _maxMistakesToTrack)
        {
            _mistakes.Add(
                new CharacterMistake(position, expected, typed));
        }

        return false;
    }

    public RaceResult GetResult(TimeSpan elapsedTime)
    {
        if (elapsedTime < TimeSpan.Zero)
        {
            throw new ArgumentException(
                "Elapsed time cannot be negative.",
                nameof(elapsedTime));
        }

        double wpm;
        if (elapsedTime.TotalMinutes > 0)
        {
            wpm = (Snippet.CodeText.Length / 5.0) / elapsedTime.TotalMinutes;
        }
        else
        {
            wpm = 0;
        }

        double accuracy;
        if (TotalKeystrokes > 0)
        {
            accuracy = (double)(TotalKeystrokes - _totalErrorsCount) / TotalKeystrokes * 100;
        }
        else
        {
            accuracy = 100;
        }

        return new RaceResult(elapsedTime, wpm, accuracy);
    }
}



