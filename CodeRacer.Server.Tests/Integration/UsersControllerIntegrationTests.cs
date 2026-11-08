using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CodeRacer.Server.Dtos;

namespace CodeRacer.Server.Tests.Integration;

/// <summary>
/// Integration tests for UsersController.
/// Runs the real ASP.NET Core pipeline (routing, validation, JSON, DI, JWT) in memory
/// with WebApplicationFactory, against an EF Core in-memory database.
/// </summary>
public class UsersControllerIntegrationTests : IClassFixture<CodeRacerWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UsersControllerIntegrationTests(CodeRacerWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidRequest_Returns201()
    {
        // Arrange
        var request = new RegisterUserRequest(
            "testuser" + Guid.NewGuid().ToString()[..8],
            "test" + Guid.NewGuid().ToString()[..8] + "@example.com",
            "ValidPassword123!",
            "ValidPassword123!"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.NotNull(result.User);
        Assert.Equal(request.UserName, result.User.UserName);
        Assert.Equal(request.Email, result.User.Email);
    }

    [Fact]
    public async Task Register_InvalidRequest_Returns400()
    {
        // Arrange
        var request = new RegisterUserRequest(
            "testuser",
            "test@example.com",
            "short",
            "short"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    public async Task Register_InvalidEmail_Returns400WithFieldError(string email)
    {
        // Arrange
        var request = new RegisterUserRequest("user" + Guid.NewGuid().ToString()[..8], email, "ValidPassword123!", "ValidPassword123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/register", request);

        // Assert - [ApiController] rejects it before the service runs
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Email", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Register_DuplicateUsername_Returns400()
    {
        // Arrange
        var username = "uniqueuser" + Guid.NewGuid().ToString()[..8];
        var request1 = new RegisterUserRequest(
            username,
            "first@example.com",
            "ValidPassword123!",
            "ValidPassword123!"
        );
        var request2 = new RegisterUserRequest(
            username,
            "second@example.com",
            "ValidPassword123!",
            "ValidPassword123!"
        );

        // Act
        var response1 = await _client.PostAsJsonAsync("/api/users/register", request1);
        var response2 = await _client.PostAsJsonAsync("/api/users/register", request2);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);
    }

    [Fact]
    public async Task Login_CorrectCredentials_Returns200()
    {
        // Arrange - Register first
        var username = "loginuser" + Guid.NewGuid().ToString()[..8];
        var email = "login" + Guid.NewGuid().ToString()[..8] + "@example.com";
        var password = "ValidPassword123!";
        var registerRequest = new RegisterUserRequest(username, email, password, password);
        await _client.PostAsJsonAsync("/api/users/register", registerRequest);

        // Act - Login
        var loginRequest = new LoginRequest(username, password);
        var response = await _client.PostAsJsonAsync("/api/users/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.Equal(username, result.User.UserName);
    }

    [Fact]
    public async Task Login_IncorrectPassword_Returns401()
    {
        // Arrange - Register first
        var username = "wrongpass" + Guid.NewGuid().ToString()[..8];
        var email = "wrong" + Guid.NewGuid().ToString()[..8] + "@example.com";
        var registerRequest = new RegisterUserRequest(username, email, "ValidPassword123!", "ValidPassword123!");
        await _client.PostAsJsonAsync("/api/users/register", registerRequest);

        // Act - Login with wrong password
        var loginRequest = new LoginRequest(username, "WrongPassword123!");
        var response = await _client.PostAsJsonAsync("/api/users/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_ExistingUser_Returns200WithoutEmail()
    {
        // Arrange - Register and get user ID
        var registerRequest = new RegisterUserRequest(
            "profileuser" + Guid.NewGuid().ToString()[..8],
            "profile" + Guid.NewGuid().ToString()[..8] + "@example.com",
            "ValidPassword123!",
            "ValidPassword123!"
        );
        var registerResponse = await _client.PostAsJsonAsync("/api/users/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();

        // Act
        var response = await _client.GetAsync($"/api/users/{authResponse!.User.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(registerRequest.Email, json);
        var profile = await response.Content.ReadFromJsonAsync<PublicUserDto>();
        Assert.NotNull(profile);
        Assert.Equal(authResponse.User.Id, profile.Id);
        Assert.Equal(authResponse.User.UserName, profile.UserName);
    }

    [Fact]
    public async Task GetOwnProfile_WithToken_Returns200WithEmail()
    {
        // Arrange
        var registerRequest = new RegisterUserRequest(
            "meuser" + Guid.NewGuid().ToString()[..8],
            "me" + Guid.NewGuid().ToString()[..8] + "@example.com",
            "ValidPassword123!",
            "ValidPassword123!"
        );
        var registerResponse = await _client.PostAsJsonAsync("/api/users/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResponse!.Token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal(authResponse.User.Id, me!.Id);
        Assert.Equal(registerRequest.Email, me.Email);
    }

    [Fact]
    public async Task GetOwnProfile_NoToken_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_NonExistentUser_Returns404()
    {
        // Act
        var response = await _client.GetAsync($"/api/users/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStatistics_ExistingUser_Returns200()
    {
        // Arrange - Register user
        var registerRequest = new RegisterUserRequest(
            "statsuser" + Guid.NewGuid().ToString()[..8],
            "stats" + Guid.NewGuid().ToString()[..8] + "@example.com",
            "ValidPassword123!",
            "ValidPassword123!"
        );
        var registerResponse = await _client.PostAsJsonAsync("/api/users/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();

        // Act
        var response = await _client.GetAsync($"/api/users/{authResponse!.User.Id}/statistics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stats = await response.Content.ReadFromJsonAsync<UserStatisticsDto>();
        Assert.NotNull(stats);
        Assert.Equal(authResponse.User.Id, stats.UserId);
        Assert.Equal(0, stats.TotalRaces); // No races yet
        Assert.Null(stats.FirstRaceDate);
    }
}
