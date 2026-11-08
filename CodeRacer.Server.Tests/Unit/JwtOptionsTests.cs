using System.ComponentModel.DataAnnotations;
using CodeRacer.Server.Settings;

namespace CodeRacer.Server.Tests.Unit;

/// <summary>
/// Unit tests for the JWT settings validation that runs at application startup.
/// </summary>
public class JwtOptionsTests
{
    private static List<ValidationResult> Validate(JwtOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Validate_ValidSettings_HasNoErrors()
    {
        // Arrange
        var options = new JwtOptions { SecretKey = new string('k', 32) };

        // Act
        var errors = Validate(options);

        // Assert
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("too_short_secret")]
    public void Validate_MissingOrShortSecretKey_ReportsSecretKey(string secretKey)
    {
        // Arrange
        var options = new JwtOptions { SecretKey = secretKey };

        // Act
        var errors = Validate(options);

        // Assert
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(JwtOptions.SecretKey)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(24 * 60 + 1)]
    public void Validate_ExpirationOutOfRange_ReportsExpirationMinutes(int minutes)
    {
        // Arrange
        var options = new JwtOptions { SecretKey = new string('k', 32), ExpirationMinutes = minutes };

        // Act
        var errors = Validate(options);

        // Assert
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(JwtOptions.ExpirationMinutes)));
    }
}
