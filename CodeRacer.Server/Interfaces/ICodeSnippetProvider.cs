using CodeRacer.Server.Models;

namespace CodeRacer.Server.Interfaces;

/// <summary>Read access to the code snippets stored in the database.</summary>
public interface ICodeSnippetProvider
{
    Task<IReadOnlyList<CodeSnippet>> GetSnippetsAsync(
        ProgrammingLanguage? language,
        CancellationToken cancellationToken);

    Task<CodeSnippet?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<CodeSnippet?> GetRandomAsync(
        ProgrammingLanguage? language,
        Difficulty? difficulty,
        CancellationToken cancellationToken);
}
