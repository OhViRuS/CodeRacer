using CodeRacer.Server.Data;
using CodeRacer.Server.Models;
using CodeRacer.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace CodeRacer.Server.Tests.Unit;

public sealed class JsonSnippetSeederTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("snippet-seed").FullName;

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private static CodeRacerDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<CodeRacerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private JsonSnippetSeeder CreateSeeder(CodeRacerDbContext context, string fileName)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["SnippetStorage:FilePath"] = fileName })
            .Build();
        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(_directory);
        return new JsonSnippetSeeder(context, configuration, environment);
    }

    private string WriteFile(string json, string name = "snippets.json")
    {
        File.WriteAllText(Path.Combine(_directory, name), json);
        return name;
    }

    private const string TwoSnippets = """
        [
          { "Id": "11111111-1111-1111-1111-111111111111", "CodeText": "print(1)\r\n", "Language": 2, "Difficulty": 0, "ProgrammingConcept": "Output" },
          { "Id": "22222222-2222-2222-2222-222222222222", "CodeText": "print(2)", "Language": 2, "Difficulty": 1, "ProgrammingConcept": "Output" }
        ]
        """;

    [Fact]
    public async Task SeedIfEmptyAsync_EmptyTable_InsertsFileContents()
    {
        var context = CreateContext();
        var seeder = CreateSeeder(context, WriteFile(TwoSnippets));

        int inserted = await seeder.SeedIfEmptyAsync(CancellationToken.None);

        Assert.Equal(2, inserted);
        Assert.Equal(2, await context.CodeSnippets.CountAsync());
    }

    [Fact]
    public async Task SeedIfEmptyAsync_NormalizesCodeText()
    {
        var context = CreateContext();
        var seeder = CreateSeeder(context, WriteFile(TwoSnippets));

        await seeder.SeedIfEmptyAsync(CancellationToken.None);

        var stored = await context.CodeSnippets.SingleAsync(s => s.Id == Guid.Parse("11111111-1111-1111-1111-111111111111"));
        Assert.DoesNotContain('\r', stored.CodeText);
    }

    [Fact]
    public async Task SeedIfEmptyAsync_TableAlreadyHasData_DoesNothingAndIgnoresFile()
    {
        var context = CreateContext();
        context.CodeSnippets.Add(new CodeSnippet { Id = Guid.NewGuid(), CodeText = "x", ProgrammingConcept = "Output" });
        await context.SaveChangesAsync();
        var seeder = CreateSeeder(context, "missing.json");

        int inserted = await seeder.SeedIfEmptyAsync(CancellationToken.None);

        Assert.Equal(0, inserted);
        Assert.Equal(1, await context.CodeSnippets.CountAsync());
    }

    [Fact]
    public async Task SeedIfEmptyAsync_MissingFile_ThrowsFileNotFound()
    {
        var seeder = CreateSeeder(CreateContext(), "missing.json");

        await Assert.ThrowsAsync<FileNotFoundException>(() => seeder.SeedIfEmptyAsync(CancellationToken.None));
    }

    [Fact]
    public async Task SeedIfEmptyAsync_SnippetWithoutId_Throws()
    {
        var json = """[ { "CodeText": "x", "Language": 2, "Difficulty": 0, "ProgrammingConcept": "Output" } ]""";
        var seeder = CreateSeeder(CreateContext(), WriteFile(json));

        await Assert.ThrowsAsync<InvalidOperationException>(() => seeder.SeedIfEmptyAsync(CancellationToken.None));
    }

    [Fact]
    public async Task SeedIfEmptyAsync_RunTwice_InsertsOnce()
    {
        var context = CreateContext();
        var seeder = CreateSeeder(context, WriteFile(TwoSnippets));

        await seeder.SeedIfEmptyAsync(CancellationToken.None);
        int second = await seeder.SeedIfEmptyAsync(CancellationToken.None);

        Assert.Equal(0, second);
        Assert.Equal(2, await context.CodeSnippets.CountAsync());
    }
}
