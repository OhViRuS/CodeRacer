using CodeRacer.Server.Data;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Repositories;

/// <summary>
/// Keeps the CodeSnippets table in step with the snippets that race results point to.
/// </summary>
public class SnippetRecordRepository(CodeRacerDbContext context) : Repository<CodeSnippet>(context), ISnippetRecordRepository
{
    public Task EnsureExistsAsync(CodeSnippet snippet, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snippet);

        // Insert a copy: the original belongs to the singleton file cache and must not be tracked here.
        var record = new CodeSnippet
        {
            Id = snippet.Id,
            CodeText = snippet.CodeText,
            Language = snippet.Language,
            Difficulty = snippet.Difficulty,
            ProgrammingConcept = snippet.ProgrammingConcept
        };

        return AddIfMissingAsync(record, s => s.Id == snippet.Id, cancellationToken);
    }
}
