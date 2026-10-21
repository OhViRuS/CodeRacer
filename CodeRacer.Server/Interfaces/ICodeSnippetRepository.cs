using System;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Interfaces;

public interface ICodeSnippetRepository : ICodeSnippetProvider
{
    void AddSnippet(CodeSnippet snippet, bool validateDuplicate = true);

    bool UpdateSnippet(Guid id, CodeSnippet updatedSnippet);

    bool DeleteSnippet(Guid id);
}
