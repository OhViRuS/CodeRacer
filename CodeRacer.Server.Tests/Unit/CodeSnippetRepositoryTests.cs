using CodeRacer.Server.Data;
using CodeRacer.Server.Exceptions;
using CodeRacer.Server.Models;
using CodeRacer.Server.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CodeRacer.Server.Tests.Unit;

/// <summary>Snippets are stored in the database: CRUD, filtering and random selection through EF Core.</summary>
public class CodeSnippetRepositoryTests
{
    private static CancellationToken Cancellation => CancellationToken.None;

    private static CodeRacerDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<CodeRacerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static CodeSnippetRepository CreateRepository(CodeRacerDbContext context, int seed = 1) =>
        new(context, new Random(seed));

    private static CodeSnippet Snippet(
        string code,
        ProgrammingLanguage language = ProgrammingLanguage.Python,
        Difficulty difficulty = Difficulty.Easy) =>
        new() { CodeText = code, Language = language, Difficulty = difficulty, ProgrammingConcept = "Output" };

    [Fact]
    public async Task AddSnippetAsync_AssignsIdAndPersists()
    {
        var context = CreateContext();
        var repository = CreateRepository(context);
        var snippet = Snippet("print(1)");

        await repository.AddSnippetAsync(snippet, validateDuplicate: true, Cancellation);

        Assert.NotEqual(Guid.Empty, snippet.Id);
        Assert.Equal(1, await context.CodeSnippets.CountAsync());
    }

    [Fact]
    public async Task AddSnippetAsync_DuplicateCodeText_ThrowsDuplicateSnippetException()
    {
        var repository = CreateRepository(CreateContext());
        await repository.AddSnippetAsync(Snippet("print(1)"), validateDuplicate: true, Cancellation);

        await Assert.ThrowsAsync<DuplicateSnippetException>(() =>
            repository.AddSnippetAsync(Snippet("print(1)"), validateDuplicate: true, Cancellation));
    }

    [Fact]
    public async Task AddSnippetAsync_DuplicateAllowedWhenValidationOff()
    {
        var context = CreateContext();
        var repository = CreateRepository(context);
        await repository.AddSnippetAsync(Snippet("print(1)"), validateDuplicate: true, Cancellation);

        await repository.AddSnippetAsync(Snippet("print(1)"), validateDuplicate: false, Cancellation);

        Assert.Equal(2, await context.CodeSnippets.CountAsync());
    }

    [Fact]
    public async Task GetSnippetsAsync_FiltersByLanguage()
    {
        var repository = CreateRepository(CreateContext());
        await repository.AddSnippetAsync(Snippet("print(1)"), true, Cancellation);
        await repository.AddSnippetAsync(Snippet("console.log(1);", ProgrammingLanguage.JavaScript), true, Cancellation);

        var python = await repository.GetSnippetsAsync(ProgrammingLanguage.Python, Cancellation);
        var all = await repository.GetSnippetsAsync(null, Cancellation);

        Assert.Single(python);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var repository = CreateRepository(CreateContext());

        Assert.Null(await repository.GetByIdAsync(Guid.NewGuid(), Cancellation));
    }

    [Fact]
    public async Task GetRandomAsync_RespectsLanguageAndDifficulty()
    {
        var repository = CreateRepository(CreateContext());
        await repository.AddSnippetAsync(Snippet("a", ProgrammingLanguage.Python, Difficulty.Easy), true, Cancellation);
        await repository.AddSnippetAsync(Snippet("b", ProgrammingLanguage.Python, Difficulty.Hard), true, Cancellation);
        await repository.AddSnippetAsync(Snippet("c", ProgrammingLanguage.JavaScript, Difficulty.Hard), true, Cancellation);

        for (int i = 0; i < 10; i++)
        {
            var picked = await repository.GetRandomAsync(ProgrammingLanguage.Python, Difficulty.Hard, Cancellation);
            Assert.NotNull(picked);
            Assert.Equal("b", picked.CodeText);
        }
    }

    [Fact]
    public async Task GetRandomAsync_NoMatch_ReturnsNull()
    {
        var repository = CreateRepository(CreateContext());
        await repository.AddSnippetAsync(Snippet("a"), true, Cancellation);

        Assert.Null(await repository.GetRandomAsync(ProgrammingLanguage.JavaScript, null, Cancellation));
    }

    [Fact]
    public async Task UpdateSnippetAsync_ChangesStoredValues()
    {
        var context = CreateContext();
        var repository = CreateRepository(context);
        var snippet = Snippet("print(1)");
        await repository.AddSnippetAsync(snippet, true, Cancellation);

        var updated = await repository.UpdateSnippetAsync(
            snippet.Id, Snippet("print(2)", difficulty: Difficulty.Hard), Cancellation);

        Assert.True(updated);
        var stored = await context.CodeSnippets.AsNoTracking().SingleAsync();
        Assert.Equal("print(2)", stored.CodeText);
        Assert.Equal(Difficulty.Hard, stored.Difficulty);
    }

    [Fact]
    public async Task UpdateSnippetAsync_UnknownId_ReturnsFalse()
    {
        var repository = CreateRepository(CreateContext());

        Assert.False(await repository.UpdateSnippetAsync(Guid.NewGuid(), Snippet("x"), Cancellation));
    }

    [Fact]
    public async Task DeleteSnippetAsync_RemovesSnippet()
    {
        var context = CreateContext();
        var repository = CreateRepository(context);
        var snippet = Snippet("print(1)");
        await repository.AddSnippetAsync(snippet, true, Cancellation);

        Assert.True(await repository.DeleteSnippetAsync(snippet.Id, Cancellation));
        Assert.Equal(0, await context.CodeSnippets.CountAsync());
        Assert.False(await repository.DeleteSnippetAsync(snippet.Id, Cancellation));
    }

    [Fact]
    public async Task DeleteSnippetAsync_SnippetWithRaceResults_ThrowsSnippetInUseException()
    {
        var context = CreateContext();
        var repository = CreateRepository(context);
        var snippet = Snippet("print(1)");
        await repository.AddSnippetAsync(snippet, true, Cancellation);
        context.RaceResults.Add(new RaceResult(TimeSpan.FromSeconds(30), 100, 95)
        {
            UserId = Guid.NewGuid(),
            CodeSnippetId = snippet.Id
        });
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<SnippetInUseException>(() => repository.DeleteSnippetAsync(snippet.Id, Cancellation));
        Assert.Equal(1, await context.CodeSnippets.CountAsync());
    }

    [Fact]
    public async Task GetSnippetsAsync_CancelledToken_Throws()
    {
        var repository = CreateRepository(CreateContext());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => repository.GetSnippetsAsync(null, cts.Token));
    }
}
