using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Services;
using Microsoft.Extensions.Configuration;

namespace CodeRacer.Server.Tests.Unit;

/// <summary>
/// Unit tests for AuthenticationService.
/// Tests password hashing, verification, and JWT token generation.
/// Uses IConfiguration from Microsoft.Extensions.Configuration.
/// </summary>
public class AuthenticationServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly IAuthenticationService _authenticationService;

    public AuthenticationServiceTests()
    {
        var configDict = new Dictionary<string, string?>
        {
            { "Jwt:SecretKey", "this_is_a_very_long_secret_key_for_testing_purposes_at_least_32_characters" },
            { "Jwt:Issuer", "CodeRacer" },
            { "Jwt:Audience", "CodeRacerApp" },
            { "Jwt:ExpirationMinutes", "60" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        _authenticationService = new AuthenticationService(_configuration);
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
    public void HashPassword_SamePasswordProducesDifferentHashes()
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

    #region Token Generation Tests

    [Fact]
    public void GenerateToken_ValidInput_ReturnsNonEmptyToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userName = "testuser";

        // Act
        var token = _authenticationService.GenerateToken(userId, userName);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateToken_NoConfiguredSecretKey_Throws()
    {
        // Arrange
        var configDict = new Dictionary<string, string?>
        {
            { "Jwt:Issuer", "CodeRacer" },
            { "Jwt:Audience", "CodeRacerApp" }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var service = new AuthenticationService(config);
        var userId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => service.GenerateToken(userId, "testuser"));
    }

    #endregion

    #region Token Expiration Tests

    [Fact]
    public void GetTokenExpiration_UseConfiguredValue_ReturnsCorrectTimeSpan()
    {
        // Act
        var expiration = _authenticationService.GetTokenExpiration();

        // Assert
        Assert.Equal(TimeSpan.FromHours(1), expiration);
    }

    [Fact]
    public void GetTokenExpiration_InvalidConfigValue_DefaultsToOneHour()
    {
        // Arrange
        var configDict = new Dictionary<string, string?>
        {
            { "Jwt:SecretKey", "secret_key_very_long_for_testing_purposes_at_least_32_chars" },
            { "Jwt:Issuer", "CodeRacer" },
            { "Jwt:ExpirationMinutes", "invalid" }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var service = new AuthenticationService(config);

        // Act
        var expiration = service.GetTokenExpiration();

        // Assert
        Assert.Equal(TimeSpan.FromHours(1), expiration);
    }

    #endregion
}
