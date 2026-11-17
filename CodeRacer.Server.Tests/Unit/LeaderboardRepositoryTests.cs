using CodeRacer.Server.Data;
using CodeRacer.Server.Dtos;
using CodeRacer.Server.Models;
using CodeRacer.Server.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CodeRacer.Server.Tests.Unit;

public class LeaderboardRepositoryTests
{
    [Fact]
    public async Task GetLeaderboardAsync_AggregatesUsersAndAppliesSnippetFilters()
    {
        var options = new DbContextOptionsBuilder<CodeRacerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new CodeRacerDbContext(options);

        var firstUser = new User
        {
            UserName = "first",
            Email = "first@example.com",
            PasswordHash = "hash"
        };
        var secondUser = new User
        {
            UserName = "second",
            Email = "second@example.com",
            PasswordHash = "hash"
        };
        var matchingSnippet = new CodeSnippet
        {
            Id = Guid.NewGuid(),
            CodeText = "for (var i = 0; i < 1; i++) {}",
            Language = ProgrammingLanguage.CSharp,
            Difficulty = Difficulty.Hard,
            ProgrammingConcept = "Loops"
        };
        var otherSnippet = new CodeSnippet
        {
            Id = Guid.NewGuid(),
            CodeText = "print(1)",
            Language = ProgrammingLanguage.Python,
            Difficulty = Difficulty.Easy,
            ProgrammingConcept = "Output"
        };

        context.Users.AddRange(firstUser, secondUser);
        context.CodeSnippets.AddRange(matchingSnippet, otherSnippet);
        context.RaceResults.AddRange(
            new RaceResult
            {
                UserId = firstUser.Id,
                CodeSnippetId = matchingSnippet.Id,
                TypingSpeed = 100,
                Accuracy = 90,
                CompletionTime = TimeSpan.FromSeconds(20)
            },
            new RaceResult
            {
                UserId = firstUser.Id,
                CodeSnippetId = matchingSnippet.Id,
                TypingSpeed = 120,
                Accuracy = 95,
                CompletionTime = TimeSpan.FromSeconds(18)
            },
            new RaceResult
            {
                UserId = firstUser.Id,
                CodeSnippetId = matchingSnippet.Id,
                TypingSpeed = 130,
                Accuracy = 80,
                CompletionTime = TimeSpan.FromSeconds(16)
            },
            new RaceResult
            {
                UserId = secondUser.Id,
                CodeSnippetId = otherSnippet.Id,
                TypingSpeed = 200,
                Accuracy = 99,
                CompletionTime = TimeSpan.FromSeconds(10)
            });
        await context.SaveChangesAsync();

        var repository = new RaceResultRepository(context);
        var result = await repository.GetLeaderboardAsync(
            new LeaderboardQuery(
                SortBy: LeaderboardSortBy.TypingSpeed,
                Language: ProgrammingLanguage.CSharp,
                Difficulty: Difficulty.Hard,
                Topic: "Loops"),
            CancellationToken.None);

        var row = Assert.Single(result);
        Assert.Equal(firstUser.Id, row.UserId);
        Assert.Equal("first", row.UserName);
        Assert.NotEqual(Guid.Empty, row.RaceResultId);
        Assert.Equal(130, row.TypingSpeed);
        Assert.Equal(80, row.Accuracy);
        Assert.Equal(TimeSpan.FromSeconds(16), row.CompletionTime);

        var accuracyResult = await repository.GetLeaderboardAsync(
            new LeaderboardQuery(
                SortBy: LeaderboardSortBy.Accuracy,
                Language: ProgrammingLanguage.CSharp,
                Difficulty: Difficulty.Hard,
                Topic: "Loops"),
            CancellationToken.None);

        var accuracyRow = Assert.Single(accuracyResult);
        Assert.Equal(120, accuracyRow.TypingSpeed);
        Assert.Equal(95, accuracyRow.Accuracy);
        Assert.Equal(TimeSpan.FromSeconds(18), accuracyRow.CompletionTime);
    }
}
