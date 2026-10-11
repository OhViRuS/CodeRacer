namespace CodeRacer.Server.Extensions;

public static class StringExtensions
{
    public static string NormalizeSnippet(this string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        return input
        .Replace("\r\n", "\n")
        .Replace("\r", "\n")
        .Replace("\t", "    ")
        .Trim();
    }
}