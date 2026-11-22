using CodeRacer.Server.Models;

namespace CodeRacer.Server.Interfaces;

/// <summary>Read access to the code snippets stored in the database.</summary>
public interface ICodeSnippetProvider
{
    Task<IReadOnlyList<CodeSnippet>> GetSnippetsAsync(
        ProgrammingLanguage? language,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CodeSnippet>> GetSnippetsAsync(
        ProgrammingLanguage? language,
        Difficulty? difficulty,
        string? topic,
        CancellationToken cancellationToken = default);

    Task<CodeSnippet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CodeSnippet?> GetRandomAsync(
        ProgrammingLanguage? language,
        Difficulty? difficulty,
        CancellationToken cancellationToken = default);

    Task<CodeSnippet?> GetRandomAsync(
        ProgrammingLanguage? language,
        Difficulty? difficulty,
        string? topic,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetTopicsAsync(
        ProgrammingLanguage? language = null,
        CancellationToken cancellationToken = default);
}
