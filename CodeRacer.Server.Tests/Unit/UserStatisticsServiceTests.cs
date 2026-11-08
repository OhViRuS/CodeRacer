using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using CodeRacer.Server.Services;
using CodeRacer.Server.Tests.TestDoubles;
using NSubstitute;

namespace CodeRacer.Server.Tests.Unit;

/// <summary>
/// Unit tests for UserStatisticsService.
/// Uses NSubstitute to stub the user and race result repositories.
/// </summary>
public class UserStatisticsServiceTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRaceResultRepository _raceResultRepository = Substitute.For<IRaceResultRepository>();
    private readonly IUserStatisticsService _statisticsService;

    private static CancellationToken Cancellation => CancellationToken.None;

    private static DateTime Now => FixedTimeProvider.DefaultNow.UtcDateTime;

    public UserStatisticsServiceTests()
    {
        _statisticsService = new UserStatisticsService(_userRepository, _raceResultRepository);
    }

    [Fact]
    public async Task GetUserStatisticsAsync_UserWithRaces_ReturnsCorrectStatistics()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, UserName = "testuser", Email = "test@example.com" };
        var raceResults = new List<RaceResult>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, CompletionTime = TimeSpan.FromSeconds(30), TypingSpeed = 100, Accuracy = 95, Mistakes = 2, CreatedAt = Now.AddDays(-2) },
            new() { Id = Guid.NewGuid(), UserId = userId, CompletionTime = TimeSpan.FromSeconds(25), TypingSpeed = 120, Accuracy = 96, Mistakes = 1, CreatedAt = Now.AddDays(-1) },
            new() { Id = Guid.NewGuid(), UserId = userId, CompletionTime = TimeSpan.FromSeconds(20), TypingSpeed = 150, Accuracy = 98, Mistakes = 0, CreatedAt = Now }
        };

        _userRepository.GetByIdAsync(userId, Cancellation).Returns(user);
        _raceResultRepository.GetByUserIdAsync(userId, Cancellation).Returns(raceResults);

        // Act
        var result = await _statisticsService.GetUserStatisticsAsync(userId, Cancellation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalRaces);
        Assert.Equal(123.33, result.AverageTypingSpeed, 2); // (100 + 120 + 150) / 3
        Assert.Equal(96.33, result.AverageAccuracy, 2); // (95 + 96 + 98) / 3
        Assert.Equal(150, result.BestTypingSpeed);
        Assert.Equal(TimeSpan.FromSeconds(20), result.BestCompletionTime);
        Assert.Equal(3, result.TotalMistakes); // 2 + 1 + 0
        Assert.Equal(Now.AddDays(-2), result.FirstRaceDate);
        Assert.Equal(Now, result.LastRaceDate);
    }

    [Fact]
    public async Task GetUserStatisticsAsync_UserWithoutRaces_ReturnsZerosAndNoDates()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, UserName = "testuser", Email = "test@example.com" };

        _userRepository.GetByIdAsync(userId, Cancellation).Returns(user);
        _raceResultRepository.GetByUserIdAsync(userId, Cancellation).Returns(new List<RaceResult>());

        // Act
        var result = await _statisticsService.GetUserStatisticsAsync(userId, Cancellation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalRaces);
        Assert.Equal(0.0, result.AverageTypingSpeed);
        Assert.Equal(0.0, result.AverageAccuracy);
        Assert.Equal(0, result.TotalMistakes);
        Assert.Null(result.BestCompletionTime);
        Assert.Null(result.FirstRaceDate);
        Assert.Null(result.LastRaceDate);
    }

    [Fact]
    public async Task GetUserStatisticsAsync_UserNotFound_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.GetByIdAsync(userId, Cancellation).Returns((User?)null);

        // Act
        var result = await _statisticsService.GetUserStatisticsAsync(userId, Cancellation);

        // Assert
        Assert.Null(result);
        await _raceResultRepository.DidNotReceiveWithAnyArgs().GetByUserIdAsync(default, default);
    }

    [Fact]
    public async Task GetUserStatisticsAsync_CancelledToken_ThrowsAndDoesNoWork()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _statisticsService.GetUserStatisticsAsync(Guid.NewGuid(), cts.Token));
        Assert.Empty(_userRepository.ReceivedCalls());
        Assert.Empty(_raceResultRepository.ReceivedCalls());
    }
}
