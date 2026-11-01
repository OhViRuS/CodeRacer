using System;
using System.Collections.Generic;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Interfaces;

public interface ICodeSnippetProvider
{
    IReadOnlyList<CodeSnippet> GetSnippets(ProgrammingLanguage? language = null);

    CodeSnippet? GetById(Guid id);

    CodeSnippet? GetRandom(ProgrammingLanguage? language = null, Difficulty? difficulty = null);
}
