using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using CodeRacer.Server.Services;
using CodeRacer.Server.Tests.TestDoubles;
using NSubstitute;

namespace CodeRacer.Server.Tests.Unit;

/// <summary>
/// Unit tests for UserStatisticsService.
/// The aggregation itself runs in the database (see RepositoryContractTests); these tests check
/// how the service turns the repository's figures into the response.
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
    public async Task GetUserStatisticsAsync_UserWithRaces_MapsRepositoryFigures()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.GetByIdAsync(userId, Cancellation).Returns(new User { Id = userId, UserName = "testuser" });
        var figures = new RaceStatistics(3, 123.33, 96.33, 150, TimeSpan.FromSeconds(20), 3, Now.AddDays(-2), Now);
        _raceResultRepository.GetStatisticsByUserIdAsync(userId, Cancellation).Returns(figures);

        // Act
        var result = await _statisticsService.GetUserStatisticsAsync(userId, Cancellation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(3, result.TotalRaces);
        Assert.Equal(123.33, result.AverageTypingSpeed);
        Assert.Equal(96.33, result.AverageAccuracy);
        Assert.Equal(150, result.BestTypingSpeed);
        Assert.Equal(TimeSpan.FromSeconds(20), result.BestCompletionTime);
        Assert.Equal(3, result.TotalMistakes);
        Assert.Equal(Now.AddDays(-2), result.FirstRaceDate);
        Assert.Equal(Now, result.LastRaceDate);
    }

    [Fact]
    public async Task GetUserStatisticsAsync_UserWithoutRaces_ReturnsZerosAndNoDates()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.GetByIdAsync(userId, Cancellation).Returns(new User { Id = userId, UserName = "testuser" });
        _raceResultRepository.GetStatisticsByUserIdAsync(userId, Cancellation).Returns((RaceStatistics?)null);

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
        await _raceResultRepository.DidNotReceiveWithAnyArgs().GetStatisticsByUserIdAsync(default, default);
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
