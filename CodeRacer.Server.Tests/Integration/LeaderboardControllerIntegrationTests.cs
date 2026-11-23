using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CodeRacer.Server.Data;
using CodeRacer.Server.Dtos;
using CodeRacer.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeRacer.Server.Tests.Integration;

/// <summary>
/// Runs GET /api/leaderboard through the real pipeline and database, so a query that only works on
/// EF Core InMemory fails here on PostgreSQL.
/// The database and the leaderboard cache are shared by the whole test collection, so each test uses
/// its own snippet with a unique topic (which is also part of the cache key) and removes it afterwards.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class LeaderboardControllerIntegrationTests(CodeRacerWebApplicationFactory factory) : IAsyncLifetime
{
    private const ProgrammingLanguage SnippetLanguage = ProgrammingLanguage.Python;
    private const Difficulty SnippetDifficulty = Difficulty.Easy;

    private readonly CodeRacerWebApplicationFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();
    private readonly Guid _snippetId = Guid.NewGuid();
    private readonly string _topic = $"Topic {Guid.NewGuid():N}";

    public Task InitializeAsync() => CreateSnippetAsync(_snippetId);

    public async Task DisposeAsync()
    {
        _client.Dispose();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CodeRacerDbContext>();
        var snippets = await context.CodeSnippets.Where(snippet => snippet.ProgrammingConcept == _topic).ToListAsync();
        var snippetIds = snippets.Select(snippet => snippet.Id).ToList();
        var results = await context.RaceResults.Where(result => snippetIds.Contains(result.CodeSnippetId)).ToListAsync();
        context.RaceResults.RemoveRange(results);
        context.CodeSnippets.RemoveRange(snippets);
        await context.SaveChangesAsync();
    }

    [Theory]
    [InlineData(LeaderboardSortBy.TypingSpeed, new[] { 0, 1, 2 })]
    [InlineData(LeaderboardSortBy.Accuracy, new[] { 1, 2, 0 })]
    [InlineData(LeaderboardSortBy.CompletionTime, new[] { 2, 0, 1 })]
    public async Task Get_EachSortMode_RanksPlayersInOrder(LeaderboardSortBy sortBy, int[] expectedPlayerOrder)
    {
        Guid[] players =
        [
            await SaveResultForNewPlayerAsync(typingSpeed: 120, accuracy: 80, seconds: 40),
            await SaveResultForNewPlayerAsync(typingSpeed: 100, accuracy: 99, seconds: 50),
            await SaveResultForNewPlayerAsync(typingSpeed: 80, accuracy: 90, seconds: 20)
        ];

        var entries = await GetLeaderboardAsync(sortBy);

        Assert.Equal(expectedPlayerOrder.Select(index => players[index]), entries.Select(entry => entry.UserId));
        Assert.Equal([1, 2, 3], entries.Select(entry => entry.Rank));
    }

    [Theory]
    [InlineData(LeaderboardSortBy.TypingSpeed, 110, 85, 25)]
    [InlineData(LeaderboardSortBy.Accuracy, 90, 99, 30)]
    [InlineData(LeaderboardSortBy.CompletionTime, 100, 90, 20)]
    public async Task Get_PlayerWithSeveralResults_AppearsOnceWithBestResultForThatSort(
        LeaderboardSortBy sortBy,
        double expectedSpeed,
        double expectedAccuracy,
        int expectedSeconds)
    {
        var (client, userId) = await CreatePlayerAsync();
        await SaveResultAsync(client, typingSpeed: 90, accuracy: 99, seconds: 30);
        await SaveResultAsync(client, typingSpeed: 110, accuracy: 85, seconds: 25);
        await SaveResultAsync(client, typingSpeed: 100, accuracy: 90, seconds: 20);

        var entries = await GetLeaderboardAsync(sortBy);

        var entry = Assert.Single(entries);
        Assert.Equal(userId, entry.UserId);
        Assert.Equal(expectedSpeed, entry.TypingSpeed);
        Assert.Equal(expectedAccuracy, entry.Accuracy);
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), entry.CompletionTime);
    }

    [Theory]
    [InlineData(LeaderboardSortBy.TypingSpeed, 100, 90, 30, 100, 90, 20)] // equal speed: faster time wins
    [InlineData(LeaderboardSortBy.Accuracy, 90, 95, 30, 110, 95, 30)] // equal accuracy: higher speed wins
    [InlineData(LeaderboardSortBy.CompletionTime, 90, 95, 30, 110, 95, 30)] // equal time: higher speed wins
    public async Task Get_TiedOnTheSortValue_UsesTheTieBreaker(
        LeaderboardSortBy sortBy,
        double loserSpeed,
        double loserAccuracy,
        int loserSeconds,
        double winnerSpeed,
        double winnerAccuracy,
        int winnerSeconds)
    {
        // The loser is saved first, so the earlier-result tie-break alone would rank it wrongly.
        var loser = await SaveResultForNewPlayerAsync(loserSpeed, loserAccuracy, loserSeconds);
        var winner = await SaveResultForNewPlayerAsync(winnerSpeed, winnerAccuracy, winnerSeconds);

        var entries = await GetLeaderboardAsync(sortBy);

        Assert.Equal([winner, loser], entries.Select(entry => entry.UserId));
    }

    [Fact]
    public async Task Get_SnippetId_LimitsResultsToThatSnippet()
    {
        var otherSnippetId = Guid.NewGuid();
        await CreateSnippetAsync(otherSnippetId);
        var player = await SaveResultForNewPlayerAsync(typingSpeed: 100, accuracy: 95, seconds: 30);
        var otherPlayer = await SaveResultForNewPlayerAsync(typingSpeed: 120, accuracy: 95, seconds: 25, otherSnippetId);

        var wholeTopic = await GetLeaderboardAsync(LeaderboardSortBy.TypingSpeed);
        var oneSnippet = await GetLeaderboardAsync(LeaderboardSortBy.TypingSpeed, snippetId: _snippetId);

        Assert.Equal([otherPlayer, player], wholeTopic.Select(entry => entry.UserId));
        Assert.Equal(player, Assert.Single(oneSnippet).UserId);
    }

    [Fact]
    public async Task Get_GuestResults_AreNotRanked()
    {
        var playerId = await SaveResultForNewPlayerAsync(typingSpeed: 80, accuracy: 90, seconds: 30);
        await SaveResultAsync(_client, typingSpeed: 200, accuracy: 100, seconds: 10); // no token: saved as guest

        var entries = await GetLeaderboardAsync(LeaderboardSortBy.TypingSpeed);

        Assert.Equal(playerId, Assert.Single(entries).UserId);
    }

    [Theory]
    [InlineData("upper")]
    [InlineData("lower")]
    [InlineData("spaces")]
    public async Task Get_TopicDifferingInCaseOrSpaces_FindsTheSameResults(string variant)
    {
        var playerId = await SaveResultForNewPlayerAsync(typingSpeed: 100, accuracy: 95, seconds: 30);
        var topic = variant switch
        {
            "upper" => _topic.ToUpperInvariant(),
            "lower" => _topic.ToLowerInvariant(),
            _ => $"  {_topic}  "
        };

        var entries = await GetLeaderboardAsync(LeaderboardSortBy.TypingSpeed, topic: topic);

        Assert.Equal(playerId, Assert.Single(entries).UserId);
    }

    [Fact]
    public async Task Get_OtherLanguageDifficultyOrTopic_ExcludesTheResult()
    {
        await SaveResultForNewPlayerAsync(typingSpeed: 100, accuracy: 95, seconds: 30);

        var otherLanguage = await GetLeaderboardAsync(LeaderboardSortBy.TypingSpeed, language: ProgrammingLanguage.CSharp);
        var otherDifficulty = await GetLeaderboardAsync(LeaderboardSortBy.TypingSpeed, difficulty: Difficulty.Hard);
        var otherTopic = await GetLeaderboardAsync(LeaderboardSortBy.TypingSpeed, topic: $"Other {_topic}");

        Assert.Empty(otherLanguage);
        Assert.Empty(otherDifficulty);
        Assert.Empty(otherTopic);
    }

    [Fact]
    public async Task Get_WithLimit_ReturnsOnlyTheTopPlayers()
    {
        var fastest = await SaveResultForNewPlayerAsync(typingSpeed: 120, accuracy: 90, seconds: 20);
        var second = await SaveResultForNewPlayerAsync(typingSpeed: 100, accuracy: 90, seconds: 25);
        await SaveResultForNewPlayerAsync(typingSpeed: 80, accuracy: 90, seconds: 30);

        var entries = await GetLeaderboardAsync(LeaderboardSortBy.TypingSpeed, limit: 2);

        Assert.Equal([fastest, second], entries.Select(entry => entry.UserId));
    }

    [Fact]
    public async Task Get_SameQueryAgain_IsServedFromTheSharedCache()
    {
        var firstPlayer = await SaveResultForNewPlayerAsync(typingSpeed: 100, accuracy: 95, seconds: 30);
        var first = await GetLeaderboardAsync(LeaderboardSortBy.TypingSpeed);

        await SaveResultForNewPlayerAsync(typingSpeed: 120, accuracy: 95, seconds: 25);
        var second = await GetLeaderboardAsync(LeaderboardSortBy.TypingSpeed);

        Assert.Equal(firstPlayer, Assert.Single(first).UserId);
        Assert.Equal(firstPlayer, Assert.Single(second).UserId);
    }

    [Theory]
    [InlineData("language=Python&difficulty=Easy")]
    [InlineData("sortBy=TypingSpeed&difficulty=Easy")]
    [InlineData("sortBy=TypingSpeed&language=Python")]
    [InlineData("sortBy=NotASort&language=Python&difficulty=Easy")]
    [InlineData("sortBy=TypingSpeed&language=NotALanguage&difficulty=Easy")]
    [InlineData("sortBy=TypingSpeed&language=Python&difficulty=NotADifficulty")]
    [InlineData("sortBy=TypingSpeed&language=Python&difficulty=Easy&limit=0")]
    [InlineData("sortBy=TypingSpeed&language=Python&difficulty=Easy&limit=101")]
    [InlineData("sortBy=CompletionTime&language=Python&difficulty=Easy")]
    public async Task Get_InvalidQuery_Returns400(string queryString)
    {
        var response = await _client.GetAsync($"/api/leaderboard?{queryString}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<IReadOnlyList<LeaderboardEntryDto>> GetLeaderboardAsync(
        LeaderboardSortBy sortBy,
        ProgrammingLanguage language = SnippetLanguage,
        Difficulty difficulty = SnippetDifficulty,
        string? topic = null,
        int limit = 10,
        Guid? snippetId = null)
    {
        // Like the leaderboard UI, sends no snippetId unless the sort requires one.
        var effectiveSnippetId = sortBy == LeaderboardSortBy.CompletionTime ? snippetId ?? _snippetId : snippetId;
        var url = $"/api/leaderboard?sortBy={sortBy}&language={language}&difficulty={difficulty}"
            + $"&topic={Uri.EscapeDataString(topic ?? _topic)}&limit={limit}";
        if (effectiveSnippetId is not null)
        {
            url += $"&snippetId={effectiveSnippetId}";
        }

        var entries = await _client.GetFromJsonAsync<List<LeaderboardEntryDto>>(url);

        Assert.NotNull(entries);
        return entries;
    }

    private async Task CreateSnippetAsync(Guid snippetId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CodeRacerDbContext>();
        context.CodeSnippets.Add(new CodeSnippet
        {
            Id = snippetId,
            CodeText = $"print('{snippetId}')",
            Language = SnippetLanguage,
            Difficulty = SnippetDifficulty,
            ProgrammingConcept = _topic
        });
        await context.SaveChangesAsync();
    }

    private async Task<Guid> SaveResultForNewPlayerAsync(
        double typingSpeed,
        double accuracy,
        int seconds,
        Guid? snippetId = null)
    {
        var (client, userId) = await CreatePlayerAsync();
        await SaveResultAsync(client, typingSpeed, accuracy, seconds, snippetId);
        return userId;
    }

    private async Task<(HttpClient Client, Guid UserId)> CreatePlayerAsync()
    {
        var client = _factory.CreateClient();
        var suffix = Guid.NewGuid().ToString()[..8];
        var register = new RegisterUserRequest(
            "racer" + suffix,
            "racer" + suffix + "@example.com",
            "ValidPassword123!",
            "ValidPassword123!");

        var response = await client.PostAsJsonAsync("/api/users/register", register);
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(auth);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return (client, auth.User.Id);
    }

    private async Task SaveResultAsync(
        HttpClient client,
        double typingSpeed,
        double accuracy,
        int seconds,
        Guid? snippetId = null)
    {
        var request = new SaveRaceResultRequest(
            snippetId ?? _snippetId,
            TimeSpan.FromSeconds(seconds),
            typingSpeed,
            accuracy,
            0);

        var response = await client.PostAsJsonAsync("/api/raceresults", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
