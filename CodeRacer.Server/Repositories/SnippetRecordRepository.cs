using CodeRacer.Server.Data;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeRacer.Server.Repositories;

/// <summary>
/// Keeps the CodeSnippets table in step with the snippets that race results point to.
/// </summary>
public class SnippetRecordRepository(CodeRacerDbContext context) : Repository<CodeSnippet>(context), ISnippetRecordRepository
{
    public async Task EnsureExistsAsync(CodeSnippet snippet, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snippet);
        cancellationToken.ThrowIfCancellationRequested();

        if (await ExistsAsync(snippet.Id, cancellationToken))
        {
            return;
        }

        // Insert a copy: the original belongs to the singleton file cache and must not be tracked here.
        var record = new CodeSnippet
        {
            Id = snippet.Id,
            CodeText = snippet.CodeText,
            Language = snippet.Language,
            Difficulty = snippet.Difficulty,
            ProgrammingConcept = snippet.ProgrammingConcept
        };

        DbSet.Add(record);
        try
        {
            await Context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Another request inserted the same snippet between the check and the insert.
            if (!await ExistsAsync(snippet.Id, cancellationToken))
            {
                throw;
            }
        }
        finally
        {
            // Never leave the record tracked: after a failed insert it would be retried on the next save.
            Context.Entry(record).State = EntityState.Detached;
        }
    }

    private Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        DbSet.AsNoTracking().AnyAsync(s => s.Id == id, cancellationToken);
}
