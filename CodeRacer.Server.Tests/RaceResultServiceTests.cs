using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeRacer.Server.Data;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using CodeRacer.Server.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CodeRacer.Server.Tests;

public class RaceResultServiceTests
{
    private static CodeRacerDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CodeRacerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CodeRacerDbContext(options);
    }

    private class FakeSnippetProvider : ICodeSnippetProvider
    {
        private readonly List<CodeSnippet> _snippets = new();

        public FakeSnippetProvider(IEnumerable<CodeSnippet>? snippets = null)
        {
            if (snippets != null)
            {
                _snippets.AddRange(snippets);
            }
        }

        public IReadOnlyList<CodeSnippet> GetSnippets(ProgrammingLanguage? language = null)
        {
            if (language != null)
            {
                return _snippets.Where(s => s.Language == language).ToList();
            }

            return _snippets;
        }

        public CodeSnippet? GetById(Guid id)
        {
            return _snippets.FirstOrDefault(s => s.Id == id);
        }

        public CodeSnippet? GetRandom(ProgrammingLanguage? language = null, Difficulty? difficulty = null)
        {
            return _snippets.FirstOrDefault();
        }
    }

    [Fact]
    public async Task SaveResultAsync_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        using var context = CreateInMemoryDbContext();
        var service = new RaceResultService(context, new FakeSnippetProvider());

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.SaveResultAsync(null!));
    }

    [Fact]
    public async Task SaveResultAsync_WithExplicitUserAndSnippet_SavesSuccessfully()
    {
        using var context = CreateInMemoryDbContext();

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "Alice",
            Email = "alice@example.com",
            PasswordHash = "hashed"
        };
        await context.Users.AddAsync(user);

        var snippet = new CodeSnippet
        {
            Id = Guid.NewGuid(),
            CodeText = "int x = 1;",
            Language = ProgrammingLanguage.CSharp,
            Difficulty = Difficulty.Easy,
            ProgrammingConcept = "Variables"
        };
        await context.CodeSnippets.AddAsync(snippet);
        await context.SaveChangesAsync();

        var service = new RaceResultService(context, new FakeSnippetProvider(new[] { snippet }));

        var request = new CreateRaceResultRequest
        {
            UserId = user.Id,
            CodeSnippetId = snippet.Id,
            CompletionTime = TimeSpan.FromSeconds(15),
            TypingSpeed = 80.0,
            Accuracy = 98.5,
            Mistakes = 2
        };

        RaceResultResponse response = await service.SaveResultAsync(request);

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(user.Id, response.UserId);
        Assert.Equal(snippet.Id, response.CodeSnippetId);
        Assert.Equal(80.0, response.TypingSpeed);
        Assert.Equal(98.5, response.Accuracy);
        Assert.Equal(2, response.Mistakes);

        var savedInDb = await context.RaceResults.FindAsync(response.Id);
        Assert.NotNull(savedInDb);
        Assert.Equal(80.0, savedInDb.TypingSpeed);
    }

    [Fact]
    public async Task SaveResultAsync_WithNullUserId_CreatesAndUsesDefaultGuestUser()
    {
        using var context = CreateInMemoryDbContext();

        var snippet = new CodeSnippet
        {
            Id = Guid.NewGuid(),
            CodeText = "print('hello')",
            Language = ProgrammingLanguage.Python,
            Difficulty = Difficulty.Easy,
            ProgrammingConcept = "Output"
        };
        await context.CodeSnippets.AddAsync(snippet);
        await context.SaveChangesAsync();

        var service = new RaceResultService(context, new FakeSnippetProvider(new[] { snippet }));

        var request = new CreateRaceResultRequest
        {
            UserId = null,
            CodeSnippetId = snippet.Id,
            CompletionTime = TimeSpan.FromSeconds(20),
            TypingSpeed = 60.0,
            Accuracy = 95.0,
            Mistakes = 3
        };

        RaceResultResponse response = await service.SaveResultAsync(request);

        Assert.Equal(RaceResultService.DefaultGuestUserId, response.UserId);

        var guest = await context.Users.FindAsync(RaceResultService.DefaultGuestUserId);
        Assert.NotNull(guest);
        Assert.Equal("Guest", guest.UserName);
    }

    [Fact]
    public async Task SaveResultAsync_WhenSnippetExistsInProvider_SeedsSnippetToDb()
    {
        using var context = CreateInMemoryDbContext();

        var snippetId = Guid.NewGuid();
        var providerSnippet = new CodeSnippet
        {
            Id = snippetId,
            CodeText = "console.log('hi');",
            Language = ProgrammingLanguage.JavaScript,
            Difficulty = Difficulty.Easy,
            ProgrammingConcept = "Output"
        };

        var service = new RaceResultService(context, new FakeSnippetProvider(new[] { providerSnippet }));

        var request = new CreateRaceResultRequest
        {
            CodeSnippetId = snippetId,
            CompletionTime = TimeSpan.FromSeconds(10),
            TypingSpeed = 90.0,
            Accuracy = 100.0,
            Mistakes = 0
        };

        RaceResultResponse response = await service.SaveResultAsync(request);

        Assert.Equal(snippetId, response.CodeSnippetId);
        var seeded = await context.CodeSnippets.FindAsync(snippetId);
        Assert.NotNull(seeded);
        Assert.Equal(providerSnippet.CodeText, seeded.CodeText);
    }

    [Fact]
    public async Task GetHistoryAsync_OrdersByCreatedAtDescending()
    {
        using var context = CreateInMemoryDbContext();
        var service = new RaceResultService(context, new FakeSnippetProvider());

        var guest = new User
        {
            Id = RaceResultService.DefaultGuestUserId,
            UserName = "Guest",
            Email = "guest@test.local",
            PasswordHash = "test"
        };
        var snippet = new CodeSnippet
        {
            Id = Guid.NewGuid(),
            CodeText = "code",
            ProgrammingConcept = "test"
        };
        await context.Users.AddAsync(guest);
        await context.CodeSnippets.AddAsync(snippet);

        var older = new RaceResult
        {
            Id = Guid.NewGuid(),
            UserId = guest.Id,
            CodeSnippetId = snippet.Id,
            TypingSpeed = 50.0,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        var newer = new RaceResult
        {
            Id = Guid.NewGuid(),
            UserId = guest.Id,
            CodeSnippetId = snippet.Id,
            TypingSpeed = 70.0,
            CreatedAt = DateTime.UtcNow
        };
        await context.RaceResults.AddRangeAsync(older, newer);
        await context.SaveChangesAsync();

        var history = await service.GetHistoryAsync(limit: 10);

        Assert.Equal(2, history.Count);
        Assert.Equal(newer.Id, history[0].Id);
        Assert.Equal(older.Id, history[1].Id);
    }

    [Fact]
    public async Task GetHistoryAsync_FiltersByUserId()
    {
        using var context = CreateInMemoryDbContext();
        var service = new RaceResultService(context, new FakeSnippetProvider());

        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var snippetId = Guid.NewGuid();

        await context.RaceResults.AddRangeAsync(
            new RaceResult { Id = Guid.NewGuid(), UserId = userA, CodeSnippetId = snippetId, TypingSpeed = 50.0 },
            new RaceResult { Id = Guid.NewGuid(), UserId = userB, CodeSnippetId = snippetId, TypingSpeed = 60.0 },
            new RaceResult { Id = Guid.NewGuid(), UserId = userA, CodeSnippetId = snippetId, TypingSpeed = 70.0 }
        );
        await context.SaveChangesAsync();

        var historyA = await service.GetHistoryAsync(userId: userA);

        Assert.Equal(2, historyA.Count);
        Assert.All(historyA, item => Assert.Equal(userA, item.UserId));
    }

    [Fact]
    public async Task GetUserStatsAsync_WhenUserHasNoRaces_ReturnsZeroedStats()
    {
        using var context = CreateInMemoryDbContext();
        var service = new RaceResultService(context, new FakeSnippetProvider());

        var randomUserId = Guid.NewGuid();
        var stats = await service.GetUserStatsAsync(randomUserId);

        Assert.NotNull(stats);
        Assert.Equal(randomUserId, stats.UserId);
        Assert.Equal(0, stats.TotalRaces);
        Assert.Equal(0, stats.BestTypingSpeed);
        Assert.Equal(0, stats.AverageTypingSpeed);
        Assert.Equal(100, stats.AverageAccuracy);
        Assert.Equal(TimeSpan.Zero, stats.TotalTimeSpent);
    }

    [Fact]
    public async Task GetUserStatsAsync_WhenUserHasRaces_ComputesCorrectAggregates()
    {
        using var context = CreateInMemoryDbContext();
        var service = new RaceResultService(context, new FakeSnippetProvider());

        var userId = Guid.NewGuid();
        var snippetId = Guid.NewGuid();

        await context.RaceResults.AddRangeAsync(
            new RaceResult
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CodeSnippetId = snippetId,
                TypingSpeed = 60.0,
                Accuracy = 90.0,
                Mistakes = 4,
                CompletionTime = TimeSpan.FromSeconds(20)
            },
            new RaceResult
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CodeSnippetId = snippetId,
                TypingSpeed = 80.0,
                Accuracy = 100.0,
                Mistakes = 0,
                CompletionTime = TimeSpan.FromSeconds(15)
            }
        );
        await context.SaveChangesAsync();

        var stats = await service.GetUserStatsAsync(userId);

        Assert.NotNull(stats);
        Assert.Equal(userId, stats.UserId);
        Assert.Equal(2, stats.TotalRaces);
        Assert.Equal(80.0, stats.BestTypingSpeed);
        Assert.Equal(70.0, stats.AverageTypingSpeed);
        Assert.Equal(95.0, stats.AverageAccuracy);
        Assert.Equal(4, stats.TotalMistakes);
        Assert.Equal(TimeSpan.FromSeconds(35), stats.TotalTimeSpent);
    }
}
