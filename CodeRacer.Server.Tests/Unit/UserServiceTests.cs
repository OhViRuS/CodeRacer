using CodeRacer.Server.Dtos;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using CodeRacer.Server.Services;
using NSubstitute;

namespace CodeRacer.Server.Tests.Unit;

/// <summary>
/// Unit tests for UserService.
/// Uses NSubstitute to mock IUserRepository and IAuthenticationService dependencies.
/// All methods are async and properly await service calls.
/// </summary>
public class UserServiceTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRaceResultRepository _raceResultRepository = Substitute.For<IRaceResultRepository>();
    private readonly IAuthenticationService _authenticationService = Substitute.For<IAuthenticationService>();
    private readonly IUserService _userService;

    private static CancellationToken Cancellation => CancellationToken.None;

    public UserServiceTests()
    {
        _userService = new UserService(_userRepository, _raceResultRepository, _authenticationService);
    }

    #region Registration Tests

    [Fact]
    public async Task RegisterAsync_ValidRequest_CreatesUserAndReturnsAuthenticationResponse()
    {
        // Arrange
        var request = new RegisterUserRequest("testuser", "test@example.com", "ValidPass123", "ValidPass123");
        var createdUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = request.UserName,
            Email = request.Email,
            PasswordHash = "hashed_password",
            CreatedAt = DateTime.UtcNow
        };

        _userRepository.UserNameExistsAsync(request.UserName, Cancellation).Returns(false);
        _userRepository.EmailExistsAsync(request.Email, Cancellation).Returns(false);
        _userRepository.AddAsync(Arg.Any<User>(), Cancellation).Returns(createdUser);
        _authenticationService.HashPassword(request.Password).Returns("hashed_password");
        _authenticationService.GenerateToken(createdUser.Id, createdUser.UserName).Returns("token123");
        _authenticationService.GetTokenExpiration().Returns(TimeSpan.FromHours(1));

        // Act
        var result = await _userService.RegisterAsync(request, Cancellation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("token123", result.Token);
        Assert.Equal(createdUser.Id, result.User.Id);
        Assert.Equal(request.UserName, result.User.UserName);
        Assert.Equal(request.Email, result.User.Email);
        await _userRepository.Received(1).AddAsync(Arg.Any<User>(), Cancellation);
    }

    [Fact]
    public async Task RegisterAsync_UserNameAlreadyExists_ReturnsNull()
    {
        // Arrange
        var request = new RegisterUserRequest("existing", "new@example.com", "ValidPass123", "ValidPass123");
        _userRepository.UserNameExistsAsync(request.UserName, Cancellation).Returns(true);

        // Act
        var result = await _userService.RegisterAsync(request, Cancellation);

        // Assert
        Assert.Null(result);
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Cancellation);
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyExists_ReturnsNull()
    {
        // Arrange
        var request = new RegisterUserRequest("newuser", "existing@example.com", "ValidPass123", "ValidPass123");
        _userRepository.UserNameExistsAsync(request.UserName, Cancellation).Returns(false);
        _userRepository.EmailExistsAsync(request.Email, Cancellation).Returns(true);

        // Act
        var result = await _userService.RegisterAsync(request, Cancellation);

        // Assert
        Assert.Null(result);
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Cancellation);
    }

    [Theory]
    [InlineData("", "test@example.com", "Password123", "Password123")] // Empty username
    [InlineData("testuser", "", "Password123", "Password123")] // Empty email
    [InlineData("testuser", "test@example.com", "", "")] // Empty password
    [InlineData("testuser", "test@example.com", "Pass123", "DifferentPass")]  // Password mismatch
    [InlineData("testuser", "test@example.com", "short", "short")] // Password too short
    public async Task RegisterAsync_InvalidRequest_ReturnsNull(string username, string email, string password, string confirmPassword)
    {
        // Arrange
        var request = new RegisterUserRequest(username, email, password, confirmPassword);

        // Act
        var result = await _userService.RegisterAsync(request, Cancellation);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task LoginAsync_CorrectCredentialsByUsername_ReturnsAuthenticationResponse()
    {
        // Arrange
        var request = new LoginRequest("testuser", "password123");
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "test@example.com",
            PasswordHash = "hashed_password",
            CreatedAt = DateTime.UtcNow
        };

        _userRepository.FindByUserNameAsync(request.UserNameOrEmail, Cancellation).Returns(user);
        _authenticationService.VerifyPassword(request.Password, user.PasswordHash).Returns(true);
        _authenticationService.GenerateToken(user.Id, user.UserName).Returns("token123");
        _authenticationService.GetTokenExpiration().Returns(TimeSpan.FromHours(1));

        // Act
        var result = await _userService.LoginAsync(request, Cancellation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("token123", result.Token);
        Assert.Equal(user.Id, result.User.Id);
        Assert.Equal(user.UserName, result.User.UserName);
    }

    [Fact]
    public async Task LoginAsync_CorrectCredentialsByEmail_ReturnsAuthenticationResponse()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "password123");
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "test@example.com",
            PasswordHash = "hashed_password",
            CreatedAt = DateTime.UtcNow
        };

        _userRepository.FindByUserNameAsync(request.UserNameOrEmail, Cancellation).Returns((User?)null);
        _userRepository.FindByEmailAsync(request.UserNameOrEmail, Cancellation).Returns(user);
        _authenticationService.VerifyPassword(request.Password, user.PasswordHash).Returns(true);
        _authenticationService.GenerateToken(user.Id, user.UserName).Returns("token123");
        _authenticationService.GetTokenExpiration().Returns(TimeSpan.FromHours(1));

        // Act
        var result = await _userService.LoginAsync(request, Cancellation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("token123", result.Token);
    }

    [Fact]
    public async Task LoginAsync_IncorrectPassword_ReturnsNull()
    {
        // Arrange
        var request = new LoginRequest("testuser", "wrongpassword");
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "test@example.com",
            PasswordHash = "hashed_password"
        };

        _userRepository.FindByUserNameAsync(request.UserNameOrEmail, Cancellation).Returns(user);
        _authenticationService.VerifyPassword(request.Password, user.PasswordHash).Returns(false);

        // Act
        var result = await _userService.LoginAsync(request, Cancellation);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsNull()
    {
        // Arrange
        var request = new LoginRequest("nonexistent", "password123");
        _userRepository.FindByUserNameAsync(request.UserNameOrEmail, Cancellation).Returns((User?)null);
        _userRepository.FindByEmailAsync(request.UserNameOrEmail, Cancellation).Returns((User?)null);

        // Act
        var result = await _userService.LoginAsync(request, Cancellation);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Profile Tests

    [Fact]
    public async Task GetUserProfileAsync_UserExists_ReturnsUserDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow
        };

        _userRepository.GetByIdAsync(userId, Cancellation).Returns(user);

        // Act
        var result = await _userService.GetUserProfileAsync(userId, Cancellation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("testuser", result.UserName);
        Assert.Equal("test@example.com", result.Email);
    }

    [Fact]
    public async Task GetUserProfileAsync_UserNotFound_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.GetByIdAsync(userId, Cancellation).Returns((User?)null);

        // Act
        var result = await _userService.GetUserProfileAsync(userId, Cancellation);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public async Task GetUserStatisticsAsync_UserWithRaces_ReturnsCorrectStatistics()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, UserName = "testuser", Email = "test@example.com" };
        var raceResults = new List<RaceResult>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, CompletionTime = TimeSpan.FromSeconds(30), TypingSpeed = 100, Accuracy = 95, Mistakes = 2, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new() { Id = Guid.NewGuid(), UserId = userId, CompletionTime = TimeSpan.FromSeconds(25), TypingSpeed = 120, Accuracy = 96, Mistakes = 1, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = Guid.NewGuid(), UserId = userId, CompletionTime = TimeSpan.FromSeconds(20), TypingSpeed = 150, Accuracy = 98, Mistakes = 0, CreatedAt = DateTime.UtcNow }
        };

        _userRepository.GetByIdAsync(userId, Cancellation).Returns(user);
        _raceResultRepository.GetByUserIdAsync(userId, Cancellation).Returns(raceResults);

        // Act
        var result = await _userService.GetUserStatisticsAsync(userId, Cancellation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalRaces);
        Assert.Equal(123.33, result.AverageTypingSpeed, 2); // (100 + 120 + 150) / 3
        Assert.Equal(96.33, result.AverageAccuracy, 2); // (95 + 96 + 98) / 3
        Assert.Equal(150, result.BestTypingSpeed);
        Assert.Equal(TimeSpan.FromSeconds(20), result.BestCompletionTime);
        Assert.Equal(3, result.TotalMistakes); // 2 + 1 + 0
    }

    [Fact]
    public async Task GetUserStatisticsAsync_UserWithoutRaces_ReturnsZeroStatistics()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, UserName = "testuser", Email = "test@example.com" };
        var emptyRaceResults = new List<RaceResult>();

        _userRepository.GetByIdAsync(userId, Cancellation).Returns(user);
        _raceResultRepository.GetByUserIdAsync(userId, Cancellation).Returns(emptyRaceResults);

        // Act
        var result = await _userService.GetUserStatisticsAsync(userId, Cancellation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalRaces);
        Assert.Equal(0.0, result.AverageTypingSpeed);
        Assert.Equal(0.0, result.AverageAccuracy);
        Assert.Equal(0, result.TotalMistakes);
    }

    [Fact]
    public async Task GetUserStatisticsAsync_UserNotFound_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.GetByIdAsync(userId, Cancellation).Returns((User?)null);

        // Act
        var result = await _userService.GetUserStatisticsAsync(userId, Cancellation);

        // Assert
        Assert.Null(result);
    }

    #endregion
}
