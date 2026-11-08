using System.Net;
using System.Net.Http.Json;
using CodeRacer.Server.Dtos;
using CodeRacer.Server.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CodeRacer.Server.Tests.Integration;

/// <summary>
/// Integration tests for UsersController.
/// Tests the real ASP.NET Core application with PostgreSQL database.
/// Uses WebApplicationFactory and makes actual HTTP requests.
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
    public async Task GetProfile_ExistingUser_Returns200()
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
        var userDto = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(userDto);
        Assert.Equal(authResponse.User.Id, userDto.Id);
        Assert.Equal(authResponse.User.UserName, userDto.UserName);
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
    }
}
