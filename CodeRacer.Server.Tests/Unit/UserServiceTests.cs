using CodeRacer.Server.Dtos;
using CodeRacer.Server.Exceptions;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using CodeRacer.Server.Services;
using CodeRacer.Server.Tests.TestDoubles;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CodeRacer.Server.Tests.Unit;

/// <summary>
/// Unit tests for UserService.
/// Uses NSubstitute to mock IUserRepository and IAuthenticationService dependencies,
/// and a fixed clock so timestamps can be asserted exactly.
/// </summary>
public class UserServiceTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IAuthenticationService _authenticationService = Substitute.For<IAuthenticationService>();
    private readonly FixedTimeProvider _clock = new();
    private readonly IUserService _userService;

    private static CancellationToken Cancellation => CancellationToken.None;

    public UserServiceTests()
    {
        _userService = new UserService(_userRepository, _authenticationService, _clock);
    }

    private static AuthenticationToken Token => new("token123", FixedTimeProvider.DefaultNow.UtcDateTime.AddHours(1));

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
        _authenticationService.GenerateToken(createdUser.Id, createdUser.UserName).Returns(Token);

        // Act
        var result = await _userService.RegisterAsync(request, Cancellation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("token123", result.Token);
        Assert.Equal(createdUser.Id, result.User.Id);
        Assert.Equal(request.UserName, result.User.UserName);
        Assert.Equal(request.Email, result.User.Email);
        Assert.Equal(Token.ExpiresAt, result.ExpiresAt);
        await _userRepository.Received(1).AddAsync(Arg.Any<User>(), Cancellation);
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_StampsCreatedAtFromClock()
    {
        // Arrange
        var request = new RegisterUserRequest("testuser", "test@example.com", "ValidPass123", "ValidPass123");
        _userRepository.AddAsync(Arg.Any<User>(), Cancellation).Returns(call => call.Arg<User>());
        _authenticationService.GenerateToken(Arg.Any<Guid>(), Arg.Any<string>()).Returns(Token);

        // Act
        await _userService.RegisterAsync(request, Cancellation);

        // Assert
        await _userRepository.Received(1).AddAsync(
            Arg.Is<User>(u => u.CreatedAt == FixedTimeProvider.DefaultNow.UtcDateTime),
            Cancellation);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateDetectedOnInsert_ReturnsNull()
    {
        // Arrange - the existence checks pass, but another request wins the race to insert
        var request = new RegisterUserRequest("racer", "racer@example.com", "ValidPass123", "ValidPass123");
        _userRepository.AddAsync(Arg.Any<User>(), Cancellation)
            .ThrowsAsync(new DuplicateUserException(new Exception("unique index")));

        // Act
        var result = await _userService.RegisterAsync(request, Cancellation);

        // Assert
        Assert.Null(result);
        _authenticationService.DidNotReceiveWithAnyArgs().GenerateToken(default, default!);
    }

    [Fact]
    public async Task RegisterAsync_CancelledToken_ThrowsAndDoesNoWork()
    {
        // Arrange
        var request = new RegisterUserRequest("testuser", "test@example.com", "ValidPass123", "ValidPass123");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => _userService.RegisterAsync(request, cts.Token));
        Assert.Empty(_userRepository.ReceivedCalls());
        Assert.Empty(_authenticationService.ReceivedCalls());
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
        _authenticationService.GenerateToken(user.Id, user.UserName).Returns(Token);

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
        _authenticationService.GenerateToken(user.Id, user.UserName).Returns(Token);

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
    public async Task GetOwnProfileAsync_UserExists_ReturnsUserDtoWithEmail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, UserName = "testuser", Email = "test@example.com", CreatedAt = DateTime.UtcNow };
        _userRepository.GetByIdAsync(userId, Cancellation).Returns(user);

        // Act
        var result = await _userService.GetOwnProfileAsync(userId, Cancellation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("testuser", result.UserName);
        Assert.Equal("test@example.com", result.Email);
    }

    [Fact]
    public async Task GetOwnProfileAsync_UserNotFound_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.GetByIdAsync(userId, Cancellation).Returns((User?)null);

        // Act
        var result = await _userService.GetOwnProfileAsync(userId, Cancellation);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPublicProfileAsync_UserExists_ReturnsProfileWithoutEmail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var createdAt = FixedTimeProvider.DefaultNow.UtcDateTime;
        var user = new User { Id = userId, UserName = "testuser", Email = "test@example.com", CreatedAt = createdAt };
        _userRepository.GetByIdAsync(userId, Cancellation).Returns(user);

        // Act
        var result = await _userService.GetPublicProfileAsync(userId, Cancellation);

        // Assert
        Assert.Equal(new PublicUserDto(userId, "testuser", createdAt), result);
    }

    [Fact]
    public async Task GetPublicProfileAsync_UserNotFound_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.GetByIdAsync(userId, Cancellation).Returns((User?)null);

        // Act
        var result = await _userService.GetPublicProfileAsync(userId, Cancellation);

        // Assert
        Assert.Null(result);
    }

    #endregion
}
