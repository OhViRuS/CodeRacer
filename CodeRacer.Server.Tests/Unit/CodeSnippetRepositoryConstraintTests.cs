using CodeRacer.Server.Data;
using CodeRacer.Server.Exceptions;
using CodeRacer.Server.Models;
using CodeRacer.Server.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CodeRacer.Server.Tests.Unit;

/// <summary>
/// Database constraints behind the snippet repository: the unique index on the code and the foreign key
/// from race results. These run against SQLite (needs the Microsoft.EntityFrameworkCore.Sqlite test package)
/// because, unlike the EF Core in-memory provider, it enforces constraints the way PostgreSQL does.
/// </summary>
public sealed class CodeSnippetRepositoryConstraintTests : IDisposable
{
    private static CancellationToken Cancellation => CancellationToken.None;

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly CodeRacerDbContext _context;
    private readonly CodeSnippetRepository _repository;

    public CodeSnippetRepositoryConstraintTests()
    {
        _connection.Open();
        _context = CreateContext();
        _context.Database.EnsureCreated();
        _repository = new CodeSnippetRepository(_context, new Random(1));
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private CodeRacerDbContext CreateContext(params IInterceptor[] interceptors) =>
        new(new DbContextOptionsBuilder<CodeRacerDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(interceptors)
            .Options);

    private static CodeSnippet Snippet(string code, Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            CodeText = code,
            Language = ProgrammingLanguage.Python,
            Difficulty = Difficulty.Easy,
            ProgrammingConcept = "Output"
        };

    [Fact]
    public async Task AddSnippetAsync_SameCodePassesTheCheck_UniqueIndexResultsInDuplicateException()
    {
        await _repository.AddSnippetAsync(Snippet("print(1)"), validateDuplicate: true, Cancellation);

        // validateDuplicate: false skips the AnyAsync check, like a second request that passed it before the first one saved.
        await Assert.ThrowsAsync<DuplicateSnippetException>(() =>
            _repository.AddSnippetAsync(Snippet("print(1)"), validateDuplicate: false, Cancellation));

        Assert.Equal(1, await _context.CodeSnippets.CountAsync());
    }

    [Fact]
    public async Task UpdateSnippetAsync_CodeOfAnotherSnippet_ThrowsDuplicateException()
    {
        var first = Snippet("print(1)");
        var second = Snippet("print(2)");
        await _repository.AddSnippetAsync(first, validateDuplicate: true, Cancellation);
        await _repository.AddSnippetAsync(second, validateDuplicate: true, Cancellation);
        _context.ChangeTracker.Clear();

        await Assert.ThrowsAsync<DuplicateSnippetException>(() =>
            _repository.UpdateSnippetAsync(second.Id, Snippet("print(1)", second.Id), Cancellation));
    }

    [Fact]
    public async Task DeleteSnippetAsync_RaceResultSavedAfterTheCheck_ThrowsSnippetInUseException()
    {
        var snippet = Snippet("print(1)");
        await _repository.AddSnippetAsync(snippet, validateDuplicate: true, Cancellation);
        _context.ChangeTracker.Clear();

        // Right before the delete is saved, another request saves a race result for the snippet.
        var racingSave = new RunOnceBeforeSaveInterceptor(async () =>
        {
            using CodeRacerDbContext other = CreateContext();
            var user = new User { Id = Guid.NewGuid(), UserName = "racer", Email = "racer@example.com", PasswordHash = "hash" };
            other.Users.Add(user);
            other.RaceResults.Add(new RaceResult(TimeSpan.FromSeconds(30), 100, 95)
            {
                UserId = user.Id,
                CodeSnippetId = snippet.Id
            });
            await other.SaveChangesAsync();
        });
        using CodeRacerDbContext racingContext = CreateContext(racingSave);
        var repository = new CodeSnippetRepository(racingContext, new Random(1));

        await Assert.ThrowsAsync<SnippetInUseException>(() => repository.DeleteSnippetAsync(snippet.Id, Cancellation));

        Assert.Equal(1, await _context.CodeSnippets.CountAsync());
    }

    private sealed class RunOnceBeforeSaveInterceptor(Func<Task> action) : SaveChangesInterceptor
    {
        private bool _ran;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (!_ran)
            {
                _ran = true;
                await action();
            }

            return result;
        }
    }
}
