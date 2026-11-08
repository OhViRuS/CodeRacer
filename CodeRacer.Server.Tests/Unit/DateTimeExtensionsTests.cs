using CodeRacer.Server.Extensions;

namespace CodeRacer.Server.Tests.Unit;

/// <summary>
/// Unit tests for DateTimeExtensions.AsUtc, which keeps query-string dates acceptable to PostgreSQL.
/// </summary>
public class DateTimeExtensionsTests
{
    [Fact]
    public void AsUtc_UnspecifiedKind_KeepsValueAndMarksUtc()
    {
        // Arrange
        var value = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Unspecified);

        // Act
        var result = value.AsUtc();

        // Assert
        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(value.Ticks, result.Ticks);
    }

    [Fact]
    public void AsUtc_UtcKind_ReturnsSameValue()
    {
        // Arrange
        var value = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var result = value.AsUtc();

        // Assert
        Assert.Equal(value, result);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Fact]
    public void AsUtc_LocalKind_ConvertsToSameMomentInUtc()
    {
        // Arrange
        var value = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Local);

        // Act
        var result = value.AsUtc();

        // Assert
        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(value.ToUniversalTime(), result);
    }
}
