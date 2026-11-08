using CodeRacer.Server.Dtos;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using CodeRacer.Server.Services;
using NSubstitute;

namespace CodeRacer.Server.Tests.Unit;

/// <summary>
/// Unit tests for RaceResultService.
/// Uses NSubstitute to mock IRaceResultRepository dependency.
/// All methods are async and properly await service calls.
/// </summary>
public class RaceResultServiceTests
{
    private readonly IRaceResultRepository _raceResultRepository = Substitute.For<IRaceResultRepository>();
    private readonly IRaceResultService _raceResultService;

    private static CancellationToken Cancellation => CancellationToken.None;

    public RaceResultServiceTests()
    {
        _raceResultService = new RaceResultService(_raceResultRepository);
    }

    #region Save Race Result Tests

    [Fact]
    public async Task SaveRaceResultAsync_ValidRequest_SavesAndReturnsDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new SaveRaceResultRequest(
            Guid.NewGuid(),
            TimeSpan.FromSeconds(30),
            100.5,
            95.0,
            2
        );

        var savedResult = new RaceResult(
            completionTime: request.CompletionTime,
            typingSpeed: request.TypingSpeed,
            accuracy: request.Accuracy)
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CodeSnippetId = request.CodeSnippetId,
            Mistakes = request.Mistakes,
            CreatedAt = DateTime.UtcNow
        };

        _raceResultRepository.AddAsync(Arg.Any<RaceResult>(), Cancellation).Returns(savedResult);

        // Act
        var result = await _raceResultService.SaveRaceResultAsync(userId, request, Cancellation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(savedResult.Id, result.Id);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(request.CodeSnippetId, result.CodeSnippetId);
        Assert.Equal(request.CompletionTime, result.CompletionTime);
        Assert.Equal(request.TypingSpeed, result.TypingSpeed);
        Assert.Equal(request.Accuracy, result.Accuracy);
        Assert.Equal(request.Mistakes, result.Mistakes);
        await _raceResultRepository.Received(1).AddAsync(Arg.Any<RaceResult>(), Cancellation);
    }

    #endregion

    #region Get Race Result Tests

    [Fact]
    public async Task GetRaceResultByIdAsync_OwnedByUser_ReturnsDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var resultId = Guid.NewGuid();
        var raceResult = new RaceResult(TimeSpan.FromSeconds(30), 100.0, 95.0)
        {
            Id = resultId,
            UserId = userId,
            CodeSnippetId = Guid.NewGuid(),
            Mistakes = 2,
            CreatedAt = DateTime.UtcNow
        };

        _raceResultRepository.GetByIdAsync(resultId, Cancellation).Returns(raceResult);

        // Act
        var result = await _raceResultService.GetRaceResultByIdAsync(resultId, userId, Cancellation);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(resultId, result.Id);
        Assert.Equal(userId, result.UserId);
    }

    [Fact]
    public async Task GetRaceResultByIdAsync_NotOwnedByUser_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var resultId = Guid.NewGuid();
        var raceResult = new RaceResult(TimeSpan.FromSeconds(30), 100.0, 95.0)
        {
            Id = resultId,
            UserId = otherUserId,
            CodeSnippetId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        _raceResultRepository.GetByIdAsync(resultId, Cancellation).Returns(raceResult);

        // Act
        var result = await _raceResultService.GetRaceResultByIdAsync(resultId, userId, Cancellation);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetRaceResultByIdAsync_ResultNotFound_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var resultId = Guid.NewGuid();
        _raceResultRepository.GetByIdAsync(resultId, Cancellation).Returns((RaceResult?)null);

        // Act
        var result = await _raceResultService.GetRaceResultByIdAsync(resultId, userId, Cancellation);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserRaceResultsAsync_ReturnsAllUserResults()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var raceResults = new List<RaceResult>
        {
            new(TimeSpan.FromSeconds(30), 100.0, 95.0) { Id = Guid.NewGuid(), UserId = userId },
            new(TimeSpan.FromSeconds(25), 110.0, 96.0) { Id = Guid.NewGuid(), UserId = userId },
            new(TimeSpan.FromSeconds(20), 120.0, 97.0) { Id = Guid.NewGuid(), UserId = userId }
        };

        _raceResultRepository.GetByUserIdAsync(userId, Cancellation).Returns(raceResults);

        // Act
        var results = await _raceResultService.GetUserRaceResultsAsync(userId, Cancellation);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task GetRecentRaceResultsAsync_ReturnsSpecifiedNumberOfResults()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var raceResults = new List<RaceResult>
        {
            new(TimeSpan.FromSeconds(30), 100.0, 95.0) { Id = Guid.NewGuid(), UserId = userId },
            new(TimeSpan.FromSeconds(25), 110.0, 96.0) { Id = Guid.NewGuid(), UserId = userId }
        };

        _raceResultRepository.GetRecentByUserIdAsync(userId, 2, Cancellation).Returns(raceResults);

        // Act
        var results = await _raceResultService.GetRecentRaceResultsAsync(userId, 2, Cancellation);

        // Assert
        Assert.Equal(2, results.Count);
        await _raceResultRepository.Received(1).GetRecentByUserIdAsync(userId, 2, Cancellation);
    }

    [Fact]
    public async Task GetRaceResultsByDateRangeAsync_ReturnsResultsInRange()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;
        var raceResults = new List<RaceResult>
        {
            new(TimeSpan.FromSeconds(30), 100.0, 95.0) { Id = Guid.NewGuid(), UserId = userId, CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new(TimeSpan.FromSeconds(25), 110.0, 96.0) { Id = Guid.NewGuid(), UserId = userId, CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };

        _raceResultRepository.GetByUserIdAndDateRangeAsync(userId, from, to, Cancellation).Returns(raceResults);

        // Act
        var results = await _raceResultService.GetRaceResultsByDateRangeAsync(userId, from, to, Cancellation);

        // Assert
        Assert.Equal(2, results.Count);
        await _raceResultRepository.Received(1).GetByUserIdAndDateRangeAsync(userId, from, to, Cancellation);
    }

    #endregion

    #region Delete Race Result Tests

    [Fact]
    public async Task DeleteRaceResultAsync_OwnedByUser_DeletesAndReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var resultId = Guid.NewGuid();
        var raceResult = new RaceResult(TimeSpan.FromSeconds(30), 100.0, 95.0)
        {
            Id = resultId,
            UserId = userId,
            CodeSnippetId = Guid.NewGuid()
        };

        _raceResultRepository.GetByIdAsync(resultId, Cancellation).Returns(raceResult);
        _raceResultRepository.DeleteAsync(resultId, Cancellation).Returns(true);

        // Act
        var result = await _raceResultService.DeleteRaceResultAsync(resultId, userId, Cancellation);

        // Assert
        Assert.True(result);
        await _raceResultRepository.Received(1).DeleteAsync(resultId, Cancellation);
    }

    [Fact]
    public async Task DeleteRaceResultAsync_NotOwnedByUser_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var resultId = Guid.NewGuid();
        var raceResult = new RaceResult(TimeSpan.FromSeconds(30), 100.0, 95.0)
        {
            Id = resultId,
            UserId = otherUserId,
            CodeSnippetId = Guid.NewGuid()
        };

        _raceResultRepository.GetByIdAsync(resultId, Cancellation).Returns(raceResult);

        // Act
        var result = await _raceResultService.DeleteRaceResultAsync(resultId, userId, Cancellation);

        // Assert
        Assert.False(result);
        await _raceResultRepository.DidNotReceive().DeleteAsync(resultId, Cancellation);
    }

    [Fact]
    public async Task DeleteRaceResultAsync_ResultNotFound_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var resultId = Guid.NewGuid();
        _raceResultRepository.GetByIdAsync(resultId, Cancellation).Returns((RaceResult?)null);

        // Act
        var result = await _raceResultService.DeleteRaceResultAsync(resultId, userId, Cancellation);

        // Assert
        Assert.False(result);
        await _raceResultRepository.DidNotReceive().DeleteAsync(resultId, Cancellation);
    }

    #endregion
}
