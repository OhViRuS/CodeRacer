using System.Net;
using System.Net.Http.Json;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Tests.Integration;

/// <summary>Snippets are served from the seeded database through the real pipeline.</summary>
public class CodeSnippetsControllerIntegrationTests(CodeRacerWebApplicationFactory factory)
    : IClassFixture<CodeRacerWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAll_ReturnsSeededSnippets()
    {
        var snippets = await _client.GetFromJsonAsync<List<CodeSnippet>>("/api/codesnippets");

        Assert.NotNull(snippets);
        Assert.NotEmpty(snippets);
    }

    [Fact]
    public async Task GetRandom_ReturnsASnippet()
    {
        var response = await _client.GetAsync("/api/codesnippets/random");

        response.EnsureSuccessStatusCode();
        Assert.NotNull(await response.Content.ReadFromJsonAsync<CodeSnippet>());
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/codesnippets/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ThenCreateSameCode_Returns409()
    {
        var snippet = new CodeSnippet { CodeText = "echo " + Guid.NewGuid(), ProgrammingConcept = "Output" };

        var first = await _client.PostAsJsonAsync("/api/codesnippets", snippet);
        var second = await _client.PostAsJsonAsync("/api/codesnippets", new CodeSnippet { CodeText = snippet.CodeText, ProgrammingConcept = "Output" });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }
}
