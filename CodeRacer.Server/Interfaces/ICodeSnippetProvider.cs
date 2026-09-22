using CodeRacer.Server.Models;
using System.Collections.Generic;

namespace CodeRacer.Server.Interfaces
{
    public interface ICodeSnippetProvider
    {
        List<CodeSnippet> GetSnippets();
    }
}