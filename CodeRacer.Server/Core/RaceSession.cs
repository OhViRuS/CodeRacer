using System.Diagnostics;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Core;

public class RaceSession
{
    private readonly Stopwatch _stopwatch = new();
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
    public IReadOnlyList<CharacterMistake> Mistakes => _mistakes;
    public bool IsCompleted => CurrentPosition >= Snippet.CodeText.Length;

    public bool TypeCharacter(char typed)
    {
        if (IsCompleted) return false;
        if (!_stopwatch.IsRunning) _stopwatch.Start();

        TotalKeystrokes++;
        char expected = Snippet.CodeText[CurrentPosition];

        if (typed == expected)
        {
            CurrentPosition++;
            if (IsCompleted) _stopwatch.Stop();
            return true;
        }

        _totalErrorsCount++;

        if (_enableDetailedTracking && _mistakes.Count < _maxMistakesToTrack)
        {
            _mistakes.Add(new CharacterMistake
            {
                PositionIndex = CurrentPosition,
                ExpectedCharacter = expected,
                TypedCharacter = typed
            });
        }
        return false;
    }

    public RaceResult GetResult(bool includeDetailedStats = false)
    {
        if (!IsCompleted)
        {
            throw new InvalidOperationException("Race is not completed yet.");
        }

        TimeSpan time = _stopwatch.Elapsed;

        double wpm;
        if (time.TotalMinutes > 0)
        {
            wpm = (CurrentPosition / 5.0) / time.TotalMinutes;
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

        // Optional detailed stats could be surfaced via a separate method or property if needed
        // For now, the parameter enables future extensibility for additional calculations
        return new RaceResult(time, wpm, accuracy);
    }
}