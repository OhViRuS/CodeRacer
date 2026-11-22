namespace CodeRacer.Server.Exceptions;

/// <summary>A snippet cannot be deleted while race results still refer to it.</summary>
public class SnippetInUseException : Exception
{
    public SnippetInUseException()
        : base("The snippet cannot be deleted because race results refer to it.")
    {
    }
}
