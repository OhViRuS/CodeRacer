using CodeRacer.Server.Data;
using CodeRacer.Server.Exceptions;
using CodeRacer.Server.Extensions;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeRacer.Server.Repositories;

/// <summary>
/// Code snippets stored in the CodeSnippets table.
/// Every method is async and passes the cancellation token on to the database.
/// </summary>
public class CodeSnippetRepository(CodeRacerDbContext context, Random random)
    : Repository<CodeSnippet>(context), ICodeSnippetRepository
{
    private readonly Random _random = random;

    public Task<IReadOnlyList<CodeSnippet>> GetSnippetsAsync(
        ProgrammingLanguage? language,
        CancellationToken cancellationToken = default)
    {
        return GetSnippetsAsync(language, difficulty: null, topic: null, cancellationToken);
    }

    public async Task<IReadOnlyList<CodeSnippet>> GetSnippetsAsync(
        ProgrammingLanguage? language,
        Difficulty? difficulty,
        string? topic,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await Filter(language, difficulty, topic)
            .OrderBy(snippet => snippet.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<CodeSnippet?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await DbSet.AsNoTracking().FirstOrDefaultAsync(snippet => snippet.Id == id, cancellationToken);
    }

    public Task<CodeSnippet?> GetRandomAsync(
        ProgrammingLanguage? language,
        Difficulty? difficulty,
        CancellationToken cancellationToken = default)
    {
        return GetRandomAsync(language, difficulty, topic: null, cancellationToken);
    }

    public async Task<CodeSnippet?> GetRandomAsync(
        ProgrammingLanguage? language,
        Difficulty? difficulty,
        string? topic,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // One query: the choice is made from the same rows that were read, so a concurrent insert or
        // delete cannot make a separate count and the pick disagree.
        List<CodeSnippet> matching = await Filter(language, difficulty, topic)
            .OrderBy(snippet => snippet.Id)
            .ToListAsync(cancellationToken);

        return matching.Count == 0 ? null : matching[_random.Next(matching.Count)];
    }

    public async Task<IReadOnlyList<string>> GetTopicsAsync(
        ProgrammingLanguage? language = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await Filter(language)
            .Where(snippet => !string.IsNullOrWhiteSpace(snippet.ProgrammingConcept))
            .Select(snippet => snippet.ProgrammingConcept.Trim())
            .Distinct()
            .OrderBy(topic => topic)
            .ToListAsync(cancellationToken);
    }

    public async Task AddSnippetAsync(CodeSnippet snippet, bool validateDuplicate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snippet);
        cancellationToken.ThrowIfCancellationRequested();

        snippet.CodeText = snippet.CodeText.NormalizeSnippet();
        snippet.ProgrammingConcept = snippet.ProgrammingConcept?.Trim() ?? string.Empty;

        if (validateDuplicate &&
            await DbSet.AsNoTracking().AnyAsync(existing => existing.CodeText == snippet.CodeText, cancellationToken))
        {
            throw new DuplicateSnippetException();
        }

        if (snippet.Id == Guid.Empty)
        {
            snippet.Id = Guid.NewGuid();
        }

        try
        {
            await AddAsync(snippet, cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two requests with the same code can both pass the check above; the unique index rejects the second.
            Context.ChangeTracker.Clear();

            if (await CodeExistsAsync(snippet.CodeText, excludedId: null, cancellationToken))
            {
                throw new DuplicateSnippetException();
            }

            throw;
        }
    }

    public async Task<bool> UpdateSnippetAsync(Guid id, CodeSnippet updatedSnippet, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(updatedSnippet);
        cancellationToken.ThrowIfCancellationRequested();

        var replacement = new CodeSnippet
        {
            Id = id,
            CodeText = updatedSnippet.CodeText.NormalizeSnippet(),
            Language = updatedSnippet.Language,
            Difficulty = updatedSnippet.Difficulty,
            ProgrammingConcept = updatedSnippet.ProgrammingConcept?.Trim() ?? string.Empty
        };

        if (await CodeExistsAsync(replacement.CodeText, excludedId: id, cancellationToken))
        {
            throw new DuplicateSnippetException();
        }

        try
        {
            return await UpdateAsync(replacement, cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Changing the code to the code of another snippet violates the unique index.
            Context.ChangeTracker.Clear();

            if (await CodeExistsAsync(replacement.CodeText, excludedId: id, cancellationToken))
            {
                throw new DuplicateSnippetException();
            }

            throw;
        }
    }

    public async Task<bool> DeleteSnippetAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (await IsUsedByRaceResultsAsync(id, cancellationToken))
        {
            throw new SnippetInUseException();
        }

        try
        {
            return await DeleteAsync(id, cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A race result may have been saved between the check above and the delete;
            // the foreign key then rejects the delete. Report that as "in use" (409), not as a 500.
            Context.ChangeTracker.Clear();

            if (await IsUsedByRaceResultsAsync(id, cancellationToken))
            {
                throw new SnippetInUseException();
            }

            throw;
        }
    }

    private Task<bool> IsUsedByRaceResultsAsync(Guid id, CancellationToken cancellationToken)
    {
        return Context.RaceResults.AsNoTracking().AnyAsync(result => result.CodeSnippetId == id, cancellationToken);
    }

    private Task<bool> CodeExistsAsync(string codeText, Guid? excludedId, CancellationToken cancellationToken)
    {
        return DbSet.AsNoTracking().AnyAsync(
            snippet => snippet.CodeText == codeText && snippet.Id != excludedId,
            cancellationToken);
    }

    private IQueryable<CodeSnippet> Filter(
        ProgrammingLanguage? language,
        Difficulty? difficulty = null,
        string? topic = null)
    {
        IQueryable<CodeSnippet> query = DbSet.AsNoTracking();

        if (language is not null)
        {
            query = query.Where(snippet => snippet.Language == language);
        }

        if (difficulty is not null)
        {
            query = query.Where(snippet => snippet.Difficulty == difficulty);
        }

        if (!string.IsNullOrWhiteSpace(topic))
        {
            var normalizedTopic = topic.Trim().ToLowerInvariant();
            query = query.Where(snippet => snippet.ProgrammingConcept.Trim().ToLower() == normalizedTopic);
        }

        return query;
    }
}
