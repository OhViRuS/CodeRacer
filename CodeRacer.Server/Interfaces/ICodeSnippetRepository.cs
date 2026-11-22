using CodeRacer.Server.Exceptions;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Interfaces;

/// <summary>Write access to the code snippets stored in the database.</summary>
public interface ICodeSnippetRepository : ICodeSnippetProvider
{
    /// <exception cref="DuplicateSnippetException">
    /// <paramref name="validateDuplicate"/> is true and a snippet with the same code text exists.
    /// </exception>
    Task AddSnippetAsync(CodeSnippet snippet, bool validateDuplicate, CancellationToken cancellationToken);

    Task<bool> UpdateSnippetAsync(Guid id, CodeSnippet updatedSnippet, CancellationToken cancellationToken);

    /// <returns>False when no snippet has this id.</returns>
    /// <exception cref="SnippetInUseException">Race results still refer to the snippet.</exception>
    Task<bool> DeleteSnippetAsync(Guid id, CancellationToken cancellationToken);
}
