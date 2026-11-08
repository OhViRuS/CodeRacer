using CodeRacer.Server.Models;

namespace CodeRacer.Server.Interfaces;

/// <summary>
/// Database-side copy of code snippets.
/// Snippets live in snippets.json (see <see cref="ICodeSnippetRepository"/>); race results reference
/// them through a foreign key, so a snippet must also exist in the CodeSnippets table before a result
/// for it can be saved.
/// </summary>
public interface ISnippetRecordRepository : IRepository<CodeSnippet>
{
    /// <summary>
    /// Inserts a copy of the snippet into the database if no row with its Id exists yet.
    /// Safe to call concurrently for the same snippet.
    /// </summary>
    Task EnsureExistsAsync(CodeSnippet snippet, CancellationToken cancellationToken);
}
