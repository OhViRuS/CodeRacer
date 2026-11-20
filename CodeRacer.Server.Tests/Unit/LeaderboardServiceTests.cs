using CodeRacer.Server.Dtos;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using CodeRacer.Server.Services;
using NSubstitute;

namespace CodeRacer.Server.Tests.Unit;

public class LeaderboardServiceTests
{
    private readonly IRaceResultRepository _raceResultRepository = Substitute.For<IRaceResultRepository>();
    private readonly LeaderboardService _service;

    public LeaderboardServiceTests()
    {
        _service = new LeaderboardService(_raceResultRepository);
    }

    [Fact]
    public async Task GetAsync_TypingSpeedSortReturnsRankedEntries()
    {
        var slower = CreateRow("slower", typingSpeed: 80, accuracy: 95, completionTime: 20);
        var faster = CreateRow("faster", typingSpeed: 120, accuracy: 90, completionTime: 15);
        _raceResultRepository.GetLeaderboardAsync(Arg.Any<LeaderboardQuery>(), Arg.Any<CancellationToken>())
            .Returns(new[] { slower, faster });

        var result = await _service.GetAsync(
            new LeaderboardQuery(
                LeaderboardSortBy.TypingSpeed,
                ProgrammingLanguage.CSharp,
                Difficulty.Easy),
            CancellationToken.None);

        Assert.Collection(
            result,
            entry =>
            {
                Assert.Equal(1, entry.Rank);
                Assert.Equal("faster", entry.UserName);
                Assert.Equal(120, entry.TypingSpeed);
            },
            entry =>
            {
                Assert.Equal(2, entry.Rank);
                Assert.Equal("slower", entry.UserName);
            });
    }

    [Fact]
    public async Task GetAsync_AccuracySortUsesSpeedAsTieBreaker()
    {
        var slower = CreateRow("slower", typingSpeed: 80, accuracy: 95, completionTime: 20);
        var faster = CreateRow("faster", typingSpeed: 120, accuracy: 95, completionTime: 15);
        _raceResultRepository.GetLeaderboardAsync(Arg.Any<LeaderboardQuery>(), Arg.Any<CancellationToken>())
            .Returns(new[] { slower, faster });

        var result = await _service.GetAsync(
            new LeaderboardQuery(
                LeaderboardSortBy.Accuracy,
                ProgrammingLanguage.CSharp,
                Difficulty.Easy),
            CancellationToken.None);

        Assert.Equal("faster", result[0].UserName);
    }

    [Fact]
    public async Task GetAsync_CompletionTimeSortReturnsFastestEntriesFirst()
    {
        var slower = CreateRow("slower", typingSpeed: 120, accuracy: 95, completionTime: 20);
        var faster = CreateRow("faster", typingSpeed: 100, accuracy: 90, completionTime: 15);
        _raceResultRepository.GetLeaderboardAsync(Arg.Any<LeaderboardQuery>(), Arg.Any<CancellationToken>())
            .Returns(new[] { slower, faster });

        var result = await _service.GetAsync(
            new LeaderboardQuery(
                LeaderboardSortBy.CompletionTime,
                ProgrammingLanguage.CSharp,
                Difficulty.Easy),
            CancellationToken.None);

        Assert.Equal("faster", result[0].UserName);
        Assert.Equal(TimeSpan.FromSeconds(15), result[0].CompletionTime);
    }

    [Fact]
    public async Task GetAsync_AppliesLimit()
    {
        var rows = Enumerable.Range(1, 3)
            .Select(index => CreateRow($"user{index}", index * 10, 90, index * 10))
            .ToArray();
        _raceResultRepository.GetLeaderboardAsync(Arg.Any<LeaderboardQuery>(), Arg.Any<CancellationToken>())
            .Returns(rows);

        var result = await _service.GetAsync(
            new LeaderboardQuery(
                LeaderboardSortBy.TypingSpeed,
                Language: ProgrammingLanguage.CSharp,
                Difficulty: Difficulty.Easy,
                Limit: 2),
            CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Rank);
        Assert.Equal(2, result[1].Rank);
    }

    [Fact]
    public async Task GetAsync_TrimsTopicBeforeRepositoryQuery()
    {
        _raceResultRepository.GetLeaderboardAsync(Arg.Any<LeaderboardQuery>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<LeaderboardRow>());

        await _service.GetAsync(
            new LeaderboardQuery(
                LeaderboardSortBy.TypingSpeed,
                Language: ProgrammingLanguage.CSharp,
                Difficulty: Difficulty.Easy,
                Topic: "  Loops  "),
            CancellationToken.None);

        await _raceResultRepository.Received(1).GetLeaderboardAsync(
            Arg.Is<LeaderboardQuery>(query => query.Topic == "Loops"),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task GetAsync_InvalidLimitThrows(int limit)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.GetAsync(
                new LeaderboardQuery(
                    LeaderboardSortBy.TypingSpeed,
                    Language: ProgrammingLanguage.CSharp,
                    Difficulty: Difficulty.Easy,
                    Limit: limit),
                CancellationToken.None));

        Assert.Empty(_raceResultRepository.ReceivedCalls());
    }

    private static LeaderboardRow CreateRow(
        string userName,
        double typingSpeed,
        double accuracy,
        double completionTime) =>
        new(
            Guid.NewGuid(),
            userName,
            Guid.NewGuid(),
            TypingSpeed: typingSpeed,
            Accuracy: accuracy,
            CompletionTime: TimeSpan.FromSeconds(completionTime),
            Mistakes: 0,
            CreatedAt: DateTime.UtcNow);
}
