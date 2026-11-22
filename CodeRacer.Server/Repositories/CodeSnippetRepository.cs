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

    public async Task<IReadOnlyList<CodeSnippet>> GetSnippetsAsync(
        ProgrammingLanguage? language,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await FilterByLanguage(language)
            .OrderBy(snippet => snippet.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<CodeSnippet?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await DbSet.AsNoTracking().FirstOrDefaultAsync(snippet => snippet.Id == id, cancellationToken);
    }

    public async Task<CodeSnippet?> GetRandomAsync(
        ProgrammingLanguage? language,
        Difficulty? difficulty,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IQueryable<CodeSnippet> candidates = FilterByLanguage(language);
        if (difficulty is not null)
        {
            candidates = candidates.Where(snippet => snippet.Difficulty == difficulty);
        }

        int count = await candidates.CountAsync(cancellationToken);
        if (count == 0)
        {
            return null;
        }

        return await candidates
            .OrderBy(snippet => snippet.Id)
            .Skip(_random.Next(count))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddSnippetAsync(CodeSnippet snippet, bool validateDuplicate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snippet);
        cancellationToken.ThrowIfCancellationRequested();

        snippet.CodeText = snippet.CodeText.NormalizeSnippet();

        if (validateDuplicate &&
            await DbSet.AsNoTracking().AnyAsync(existing => existing.CodeText == snippet.CodeText, cancellationToken))
        {
            throw new DuplicateSnippetException();
        }

        if (snippet.Id == Guid.Empty)
        {
            snippet.Id = Guid.NewGuid();
        }

        await AddAsync(snippet, cancellationToken);
    }

    public Task<bool> UpdateSnippetAsync(Guid id, CodeSnippet updatedSnippet, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(updatedSnippet);

        var replacement = new CodeSnippet
        {
            Id = id,
            CodeText = updatedSnippet.CodeText.NormalizeSnippet(),
            Language = updatedSnippet.Language,
            Difficulty = updatedSnippet.Difficulty,
            ProgrammingConcept = updatedSnippet.ProgrammingConcept
        };

        return UpdateAsync(replacement, cancellationToken);
    }

    public async Task<bool> DeleteSnippetAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (await Context.RaceResults.AsNoTracking().AnyAsync(result => result.CodeSnippetId == id, cancellationToken))
        {
            throw new SnippetInUseException();
        }

        return await DeleteAsync(id, cancellationToken);
    }

    private IQueryable<CodeSnippet> FilterByLanguage(ProgrammingLanguage? language)
    {
        IQueryable<CodeSnippet> query = DbSet.AsNoTracking();
        return language is null ? query : query.Where(snippet => snippet.Language == language);
    }
}
