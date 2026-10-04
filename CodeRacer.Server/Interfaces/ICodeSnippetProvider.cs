using CodeRacer.Server.Models;
using System.Collections.Generic;

namespace CodeRacer.Server.Interfaces;

public interface ICodeSnippetProvider
{
    List<CodeSnippet> GetSnippets(string filePath = "Data/snippets.json");

    void AddSnippet(CodeSnippet snippet, bool validateDuplicate = true);

    bool UpdateSnippet(Guid id, CodeSnippet updatedSnippet);

    bool DeleteSnippet(Guid id);
}