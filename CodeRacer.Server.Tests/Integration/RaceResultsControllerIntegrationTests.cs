using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CodeRacer.Server.Dtos;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using Microsoft.Extensions.DependencyInjection;

namespace CodeRacer.Server.Tests.Integration;

/// <summary>
/// Integration tests for RaceResultsController.
/// Exercises the real ASP.NET Core pipeline including JWT authentication:
/// each test registers a user and calls the API with that user's bearer token.
/// </summary>
public class RaceResultsControllerIntegrationTests : IClassFixture<CodeRacerWebApplicationFactory>
{
    private readonly CodeRacerWebApplicationFactory _factory;

    public RaceResultsControllerIntegrationTests(CodeRacerWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, AuthenticationResponse Auth)> CreateAuthenticatedClientAsync()
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
        return (client, auth);
    }

    /// <summary>Builds a request for a snippet that really exists in snippets.json.</summary>
    private SaveRaceResultRequest CreateRequest(double typingSpeed = 100.5)
    {
        var snippetId = _factory.Services.GetRequiredService<ICodeSnippetProvider>().GetSnippets()[0].Id;
        return new SaveRaceResultRequest(snippetId, TimeSpan.FromSeconds(30), typingSpeed, 95.0, 2);
    }

    [Fact]
    public async Task SaveRaceResult_ValidRequest_Returns201()
    {
        // Arrange
        var (client, auth) = await CreateAuthenticatedClientAsync();
        var request = CreateRequest();

        // Act
        var response = await client.PostAsJsonAsync("/api/raceresults", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var result = await response.Content.ReadFromJsonAsync<RaceResultDto>();
        Assert.NotNull(result);
        Assert.Equal(auth.User.Id, result.UserId);
        Assert.Equal(request.CodeSnippetId, result.CodeSnippetId);
        Assert.Equal(request.TypingSpeed, result.TypingSpeed);
        Assert.Equal(request.Mistakes, result.Mistakes);
    }

    [Fact]
    public async Task SaveRaceResult_UnknownSnippet_Returns400()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var request = CreateRequest() with { CodeSnippetId = Guid.NewGuid() };

        var response = await client.PostAsJsonAsync("/api/raceresults", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(-1.0, 95.0)]
    [InlineData(501.0, 95.0)]
    [InlineData(1e308, 95.0)]
    [InlineData(100.0, 150.0)]
    [InlineData(100.0, 1e308)]
    public async Task SaveRaceResult_OutOfRangeValues_Returns400(double typingSpeed, double accuracy)
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var request = CreateRequest() with { TypingSpeed = typingSpeed, Accuracy = accuracy };

        var response = await client.PostAsJsonAsync("/api/raceresults", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10_001)]
    [InlineData(int.MaxValue)]
    public async Task SaveRaceResult_MistakesOutOfRange_Returns400(int mistakes)
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var request = CreateRequest() with { Mistakes = mistakes };

        var response = await client.PostAsJsonAsync("/api/raceresults", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SaveRaceResult_MaximumAllowedValues_SavedAndStatisticsStillWork()
    {
        var (client, auth) = await CreateAuthenticatedClientAsync();
        var request = CreateRequest() with { TypingSpeed = 500, Mistakes = 10_000 };

        var first = await client.PostAsJsonAsync("/api/raceresults", request);
        var second = await client.PostAsJsonAsync("/api/raceresults", request);
        var stats = await client.GetFromJsonAsync<UserStatisticsDto>($"/api/users/{auth.User.Id}/statistics");

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(500, stats!.BestTypingSpeed);
        Assert.Equal(20_000, stats.TotalMistakes);
    }

    [Theory]
    [InlineData("00:00:00")]
    [InlineData("-00:00:30")]
    public async Task SaveRaceResult_NonPositiveCompletionTime_Returns400(string completionTime)
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var request = CreateRequest() with { CompletionTime = TimeSpan.Parse(completionTime) };

        var response = await client.PostAsJsonAsync("/api/raceresults", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetRaceResultsByDateRange_DateOnlyTo_IncludesRacesFromThatDay()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/api/raceresults", CreateRequest());
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var results = await client.GetFromJsonAsync<List<RaceResultDto>>(
            $"/api/raceresults/by-date-range?from={today}&to={today}");

        Assert.Single(results!);
    }

    [Fact]
    public async Task GetRaceResultsByDateRange_MissingTo_Returns400()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/raceresults/by-date-range?from=2026-01-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetRecentRaceResults_NoCount_ReturnsUpToTen()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/api/raceresults", CreateRequest());

        var response = await client.GetAsync("/api/raceresults/recent");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<List<RaceResultDto>>();
        Assert.Single(results!);
    }

    [Fact]
    public async Task SaveRaceResult_SameSnippetTwice_BothSaved()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();

        var first = await client.PostAsJsonAsync("/api/raceresults", CreateRequest(80));
        var second = await client.PostAsJsonAsync("/api/raceresults", CreateRequest(90));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }

    [Fact]
    public async Task SaveRaceResult_NoAuth_SavesAsGuest()
    {
        var client = _factory.CreateClient();

        var first = await client.PostAsJsonAsync("/api/raceresults", CreateRequest());
        var second = await client.PostAsJsonAsync("/api/raceresults", CreateRequest());

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var result = await first.Content.ReadFromJsonAsync<RaceResultDto>();
        Assert.Equal(GuestAccount.Id, result!.UserId);
    }

    [Fact]
    public async Task SaveRaceResult_SignedIn_SavesToOwnAccountNotGuest()
    {
        var (client, auth) = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/raceresults", CreateRequest());

        var result = await response.Content.ReadFromJsonAsync<RaceResultDto>();
        Assert.Equal(auth.User.Id, result!.UserId);
        Assert.NotEqual(GuestAccount.Id, result.UserId);
    }

    [Fact]
    public async Task SaveRaceResult_InvalidToken_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");

        var response = await client.PostAsJsonAsync("/api/raceresults", CreateRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRaceResult_OwnResult_Returns200()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var saved = await (await client.PostAsJsonAsync("/api/raceresults", CreateRequest()))
            .Content.ReadFromJsonAsync<RaceResultDto>();

        var response = await client.GetAsync($"/api/raceresults/{saved!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RaceResultDto>();
        Assert.Equal(saved.Id, result!.Id);
    }

    [Fact]
    public async Task GetRaceResult_OtherUsersResult_Returns404()
    {
        var (owner, _) = await CreateAuthenticatedClientAsync();
        var (other, _) = await CreateAuthenticatedClientAsync();
        var saved = await (await owner.PostAsJsonAsync("/api/raceresults", CreateRequest()))
            .Content.ReadFromJsonAsync<RaceResultDto>();

        var response = await other.GetAsync($"/api/raceresults/{saved!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUserRaceResults_OtherUsersHaveResults_ReturnsOnlyOwn()
    {
        var (client, auth) = await CreateAuthenticatedClientAsync();
        var (other, _) = await CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/api/raceresults", CreateRequest(80));
        await client.PostAsJsonAsync("/api/raceresults", CreateRequest(90));
        await other.PostAsJsonAsync("/api/raceresults", CreateRequest(70));

        var results = await client.GetFromJsonAsync<List<RaceResultDto>>("/api/raceresults");

        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(auth.User.Id, r.UserId));
    }

    [Fact]
    public async Task GetRecentRaceResults_CountOutOfRange_Returns400()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/raceresults/recent?count=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetRaceResultsByDateRange_FromAfterTo_Returns400()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/raceresults/by-date-range?from=2026-02-01&to=2026-01-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteRaceResult_OwnResult_Returns204ThenNotFound()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var saved = await (await client.PostAsJsonAsync("/api/raceresults", CreateRequest()))
            .Content.ReadFromJsonAsync<RaceResultDto>();

        var delete = await client.DeleteAsync($"/api/raceresults/{saved!.Id}");
        var get = await client.GetAsync($"/api/raceresults/{saved.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task GetStatistics_AfterSavingResults_ReflectsThem()
    {
        var (client, auth) = await CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/api/raceresults", CreateRequest(80));
        await client.PostAsJsonAsync("/api/raceresults", CreateRequest(120));

        var stats = await client.GetFromJsonAsync<UserStatisticsDto>($"/api/users/{auth.User.Id}/statistics");

        Assert.NotNull(stats);
        Assert.Equal(2, stats.TotalRaces);
        Assert.Equal(120, stats.BestTypingSpeed);
        Assert.Equal(100, stats.AverageTypingSpeed);
    }

    [Fact]
    public async Task GetUserRaceResults_NoAuth_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/raceresults");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRaceResult_NoAuth_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync($"/api/raceresults/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
