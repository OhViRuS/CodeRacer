using System.Collections.Generic;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Interfaces;

public interface ISourceSnippetExtractor
{
    /// <summary>
    /// Extracts suitable code snippets from imported source code.
    /// </summary>
    IReadOnlyList<ExtractedSourceSnippet> Extract(
        ProgrammingLanguage language,
        string content);
}
