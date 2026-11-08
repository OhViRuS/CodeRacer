using CodeRacer.Server.Controllers;
using CodeRacer.Server.Dtos;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Tests.TestDoubles;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace CodeRacer.Server.Tests.Unit;

/// <summary>
/// Controller tests for UsersController.
/// The controller is called directly with NSubstitute doubles for IUserService and IUserStatisticsService.
/// </summary>
public class UsersControllerTests
{
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly IUserStatisticsService _statisticsService = Substitute.For<IUserStatisticsService>();
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _controller = new UsersController(_userService, _statisticsService).SignedOut();
    }

    private static readonly DateTime Now = FixedTimeProvider.DefaultNow.UtcDateTime;

    private static AuthenticationResponse CreateAuthResponse(Guid userId) =>
        new("token123", new UserDto(userId, "racer", "racer@example.com", Now), Now.AddHours(1));

    [Fact]
    public async Task Register_ValidRequest_ReturnsCreatedAtGetProfile()
    {
        // Arrange
        var request = new RegisterUserRequest("racer", "racer@example.com", "ValidPass123", "ValidPass123");
        var auth = CreateAuthResponse(Guid.NewGuid());
        _userService.RegisterAsync(request, Arg.Any<CancellationToken>()).Returns(auth);

        // Act
        var response = await _controller.Register(request, CancellationToken.None);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(response.Result);
        Assert.Equal(nameof(UsersController.GetProfile), created.ActionName);
        Assert.Equal(auth.User.Id, created.RouteValues!["userId"]);
        Assert.Same(auth, created.Value);
    }

    [Fact]
    public async Task Register_ServiceRejects_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterUserRequest("taken", "taken@example.com", "ValidPass123", "ValidPass123");
        _userService.RegisterAsync(request, Arg.Any<CancellationToken>()).Returns((AuthenticationResponse?)null);

        // Act
        var response = await _controller.Register(request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    [Fact]
    public async Task Register_ValidRequest_PassesRequestTokenToService()
    {
        // Arrange
        var request = new RegisterUserRequest("racer", "racer@example.com", "ValidPass123", "ValidPass123");
        using var cts = new CancellationTokenSource();

        // Act
        await _controller.Register(request, cts.Token);

        // Assert
        await _userService.Received(1).RegisterAsync(request, cts.Token);
    }

    [Fact]
    public async Task Login_CorrectCredentials_ReturnsOk()
    {
        // Arrange
        var request = new LoginRequest("racer", "ValidPass123");
        var auth = CreateAuthResponse(Guid.NewGuid());
        _userService.LoginAsync(request, Arg.Any<CancellationToken>()).Returns(auth);

        // Act
        var response = await _controller.Login(request, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(response.Result);
        Assert.Same(auth, ok.Value);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest("racer", "WrongPass123");
        _userService.LoginAsync(request, Arg.Any<CancellationToken>()).Returns((AuthenticationResponse?)null);

        // Act
        var response = await _controller.Login(request, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(response.Result);
    }

    [Fact]
    public async Task GetOwnProfile_SignedIn_ReturnsOwnProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new UserDto(userId, "racer", "racer@example.com", Now);
        _userService.GetOwnProfileAsync(userId, Arg.Any<CancellationToken>()).Returns(dto);
        var controller = new UsersController(_userService, _statisticsService).SignedInAs(userId);

        // Act
        var response = await controller.GetOwnProfile(CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(response.Result);
        Assert.Same(dto, ok.Value);
    }

    [Fact]
    public async Task GetOwnProfile_NoUserIdClaim_ReturnsUnauthorizedWithoutCallingService()
    {
        // Act
        var response = await _controller.GetOwnProfile(CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedResult>(response.Result);
        Assert.Empty(_userService.ReceivedCalls());
    }

    [Fact]
    public async Task GetProfile_ExistingUser_ReturnsPublicProfile()
    {
        // Arrange
        var dto = new PublicUserDto(Guid.NewGuid(), "racer", Now);
        _userService.GetPublicProfileAsync(dto.Id, Arg.Any<CancellationToken>()).Returns(dto);

        // Act
        var response = await _controller.GetProfile(dto.Id, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(response.Result);
        Assert.Same(dto, ok.Value);
    }

    [Fact]
    public async Task GetProfile_UnknownUser_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userService.GetPublicProfileAsync(userId, Arg.Any<CancellationToken>()).Returns((PublicUserDto?)null);

        // Act
        var response = await _controller.GetProfile(userId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(response.Result);
    }

    [Fact]
    public async Task GetStatistics_ExistingUser_ReturnsOkAndPassesToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var stats = new UserStatisticsDto(userId, 0, 0, 0, 0, null, 0, null, null);
        using var cts = new CancellationTokenSource();
        _statisticsService.GetUserStatisticsAsync(userId, cts.Token).Returns(stats);

        // Act
        var response = await _controller.GetStatistics(userId, cts.Token);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(response.Result);
        Assert.Same(stats, ok.Value);
        await _statisticsService.Received(1).GetUserStatisticsAsync(userId, cts.Token);
    }

    [Fact]
    public async Task GetStatistics_UnknownUser_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _statisticsService.GetUserStatisticsAsync(userId, Arg.Any<CancellationToken>()).Returns((UserStatisticsDto?)null);

        // Act
        var response = await _controller.GetStatistics(userId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(response.Result);
    }
}
