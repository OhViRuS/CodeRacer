using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Services;
using CodeRacer.Server.Settings;
using CodeRacer.Server.Tests.TestDoubles;
using Microsoft.Extensions.Options;

namespace CodeRacer.Server.Tests.Unit;

/// <summary>
/// Unit tests for AuthenticationService.
/// Tests password hashing, verification, and JWT token generation.
/// Uses a fixed clock, so token lifetimes can be checked exactly.
/// </summary>
public class AuthenticationServiceTests
{
    private readonly FixedTimeProvider _clock = new();
    private readonly JwtOptions _jwtOptions = new()
    {
        SecretKey = "this_is_a_very_long_secret_key_for_testing_purposes_at_least_32_characters",
        Issuer = "CodeRacer",
        Audience = "CodeRacerApp",
        ExpirationMinutes = 60
    };

    private readonly IAuthenticationService _authenticationService;

    public AuthenticationServiceTests()
    {
        _authenticationService = new AuthenticationService(Options.Create(_jwtOptions), _clock);
    }

    #region Password Hashing Tests

    [Fact]
    public void HashPassword_ValidPassword_ReturnsNonEmptyHash()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash = _authenticationService.HashPassword(password);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.NotEqual(password, hash); // Hash should not be plaintext
    }

    [Fact]
    public void HashPassword_SamePasswordTwice_ProducesDifferentHashes()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash1 = _authenticationService.HashPassword(password);
        var hash2 = _authenticationService.HashPassword(password);

        // Assert
        Assert.NotEqual(hash1, hash2); // BCrypt produces different hashes with salt
    }

    [Fact]
    public void HashPassword_EmptyPassword_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _authenticationService.HashPassword(""));
    }

    #endregion

    #region Password Verification Tests

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "TestPassword123!";
        var hash = _authenticationService.HashPassword(password);

        // Act
        var result = _authenticationService.VerifyPassword(password, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_IncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword123!";
        var hash = _authenticationService.HashPassword(password);

        // Act
        var result = _authenticationService.VerifyPassword(wrongPassword, hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_EmptyPassword_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var hash = _authenticationService.HashPassword(password);

        // Act
        var result = _authenticationService.VerifyPassword("", hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_EmptyHash_ReturnsFalse()
    {
        // Act
        var result = _authenticationService.VerifyPassword("Password", "");

        // Assert
        Assert.False(result);
    }

    #endregion

    [Theory]
    [InlineData("")]
    [InlineData("SomePassword123")]
    public void SimulatePasswordCheck_AnyPassword_DoesNotThrow(string password)
    {
        // Act
        var exception = Record.Exception(() => _authenticationService.SimulatePasswordCheck(password));

        // Assert
        Assert.Null(exception);
    }

    #region Token Generation Tests

    [Fact]
    public void GenerateToken_ValidInput_ReturnsNonEmptyToken()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var token = _authenticationService.GenerateToken(userId, "testuser");

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token.Token));
    }

    [Fact]
    public void GenerateToken_FixedClock_ExpiresAfterConfiguredMinutes()
    {
        // Arrange
        var expected = FixedTimeProvider.DefaultNow.UtcDateTime.AddMinutes(60);

        // Act
        var token = _authenticationService.GenerateToken(Guid.NewGuid(), "testuser");

        // Assert
        Assert.Equal(expected, token.ExpiresAt);
        Assert.Equal(expected, new JwtSecurityTokenHandler().ReadJwtToken(token.Token).ValidTo);
    }

    [Fact]
    public void GenerateToken_ValidInput_ContainsUserIdAndNameClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var token = _authenticationService.GenerateToken(userId, "testuser");

        // Assert
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Token);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId.ToString());
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Name && c.Value == "testuser");
        Assert.Equal("CodeRacer", jwt.Issuer);
        Assert.Contains("CodeRacerApp", jwt.Audiences);
    }

    [Fact]
    public void GenerateToken_NoConfiguredSecretKey_Throws()
    {
        // Arrange
        var service = new AuthenticationService(Options.Create(new JwtOptions { SecretKey = "" }), _clock);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => service.GenerateToken(Guid.NewGuid(), "testuser"));
    }

    #endregion
}
