namespace CodeRacer.Server.Models;

public class RaceResultRequest
{
    public Guid SnippetId { get; set; }
    public double ElapsedSeconds { get; set; }
    public string FinalText { get; set; } = string.Empty;
    public List<KeystrokeAttemptRequest> Keystrokes { get; set; } = new();
}

public class KeystrokeAttemptRequest
{
    public int Position { get; set; }
    public char Character { get; set; }
}
