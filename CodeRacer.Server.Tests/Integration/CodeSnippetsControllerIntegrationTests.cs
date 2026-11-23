using System.Net;
using System.Net.Http.Json;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Tests.Integration;

/// <summary>Snippets are served from the seeded database through the real pipeline.</summary>
[Collection(IntegrationTestCollection.Name)]
public class CodeSnippetsControllerIntegrationTests(CodeRacerWebApplicationFactory factory)
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
    public async Task GetAll_WithDifficultyAndTopicFilter_ReturnsMatchingSnippets()
    {
        var response = await _client.GetAsync("/api/codesnippets?difficulty=Easy&topic=Output");

        response.EnsureSuccessStatusCode();
        var snippets = await response.Content.ReadFromJsonAsync<List<CodeSnippet>>();
        Assert.NotNull(snippets);
        Assert.NotEmpty(snippets);
        Assert.All(snippets, s =>
        {
            Assert.Equal(Difficulty.Easy, s.Difficulty);
            Assert.Equal("Output", s.ProgrammingConcept, ignoreCase: true);
        });
    }

    [Fact]
    public async Task GetDifficulties_ReturnsAllDifficultyEnumNames()
    {
        var difficulties = await _client.GetFromJsonAsync<List<string>>("/api/codesnippets/difficulties");

        Assert.NotNull(difficulties);
        Assert.Equal(Enum.GetNames<Difficulty>(), difficulties);
    }

    [Fact]
    public async Task GetTopics_ReturnsSeededTopics()
    {
        var topics = await _client.GetFromJsonAsync<List<string>>("/api/codesnippets/topics");

        Assert.NotNull(topics);
        Assert.NotEmpty(topics);
        Assert.Contains("Output", topics);
    }

    [Fact]
    public async Task GetTopics_FilteredByLanguage_ReturnsOnlyThatLanguagesTopics()
    {
        var topics = await _client.GetFromJsonAsync<List<string>>("/api/codesnippets/topics?language=Python");

        Assert.NotNull(topics);
        Assert.NotEmpty(topics);
    }

    [Fact]
    public async Task GetRandom_ReturnsASnippet()
    {
        var response = await _client.GetAsync("/api/codesnippets/random");

        response.EnsureSuccessStatusCode();
        Assert.NotNull(await response.Content.ReadFromJsonAsync<CodeSnippet>());
    }

    [Fact]
    public async Task GetRandom_WithDifficultyAndTopicFilter_ReturnsMatchingSnippet()
    {
        var response = await _client.GetAsync("/api/codesnippets/random?difficulty=Easy&topic=Output");

        response.EnsureSuccessStatusCode();
        var snippet = await response.Content.ReadFromJsonAsync<CodeSnippet>();
        Assert.NotNull(snippet);
        Assert.Equal(Difficulty.Easy, snippet.Difficulty);
        Assert.Equal("Output", snippet.ProgrammingConcept, ignoreCase: true);
    }

    [Fact]
    public async Task GetRandom_NoMatch_Returns404()
    {
        var response = await _client.GetAsync("/api/codesnippets/random?topic=NonExistentTopic12345");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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

    [Fact]
    public async Task Create_WithIdOfExistingSnippet_IgnoresTheIdAndCreatesNewSnippet()
    {
        var existing = (await _client.GetFromJsonAsync<List<CodeSnippet>>("/api/codesnippets"))![0];
        var snippet = new CodeSnippet
        {
            Id = existing.Id,
            CodeText = "echo " + Guid.NewGuid(),
            ProgrammingConcept = "Output"
        };

        var response = await _client.PostAsJsonAsync("/api/codesnippets", snippet);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CodeSnippet>();
        Assert.NotNull(created);
        Assert.NotEqual(existing.Id, created.Id);
    }

    [Fact]
    public async Task Create_ConceptLongerThanColumn_Returns400()
    {
        var snippet = new CodeSnippet
        {
            CodeText = "echo " + Guid.NewGuid(),
            ProgrammingConcept = new string('x', CodeSnippet.MaxProgrammingConceptLength + 1)
        };

        var response = await _client.PostAsJsonAsync("/api/codesnippets", snippet);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_CodeTextLongerThanLimit_Returns400()
    {
        var snippet = new CodeSnippet
        {
            CodeText = new string('x', CodeSnippet.MaxCodeTextLength + 1),
            ProgrammingConcept = "Output"
        };

        var response = await _client.PostAsJsonAsync("/api/codesnippets", snippet);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_CodeTextAtLimit_Returns201()
    {
        var snippet = new CodeSnippet
        {
            CodeText = new string('x', CodeSnippet.MaxCodeTextLength),
            ProgrammingConcept = "Output"
        };

        var response = await _client.PostAsJsonAsync("/api/codesnippets", snippet);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("   \t  ")]
    [InlineData("\r\n")]
    public async Task Create_WhitespaceOnlyCodeText_Returns400(string codeText)
    {
        var snippet = new CodeSnippet { CodeText = codeText, ProgrammingConcept = "Output" };

        var response = await _client.PostAsJsonAsync("/api/codesnippets", snippet);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
