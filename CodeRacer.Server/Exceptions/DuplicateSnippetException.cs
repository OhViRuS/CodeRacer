using System;

namespace CodeRacer.Server.Exceptions;

public class DuplicateSnippetException : Exception
{
    public DuplicateSnippetException()
        : base("A snippet with identical code text already exists.")
    {
    }
}
