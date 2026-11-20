using CodeRacer.Server.Controllers;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace CodeRacer.Server.Tests.Unit;

public class LeaderboardControllerTests
{
    [Fact]
    public async Task Get_WithoutSortBy_ReturnsBadRequestWithoutCallingService()
    {
        var service = Substitute.For<ILeaderboardService>();
        var controller = new LeaderboardController(service);

        var result = await controller.Get(
            sortBy: null,
            language: ProgrammingLanguage.CSharp,
            difficulty: Difficulty.Hard,
            topic: null,
            limit: 10,
            cancellationToken: CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("SortBy", badRequest.Value?.ToString());
        Assert.Empty(service.ReceivedCalls());
    }

    [Theory]
    [InlineData(999)]
    [InlineData(-1)]
    public async Task Get_WithInvalidSortBy_ReturnsBadRequest(int sortBy)
    {
        var service = Substitute.For<ILeaderboardService>();
        var controller = new LeaderboardController(service);

        var result = await controller.Get(
            sortBy: (LeaderboardSortBy)sortBy,
            language: ProgrammingLanguage.CSharp,
            difficulty: Difficulty.Hard,
            topic: null,
            limit: 10,
            cancellationToken: CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("sortBy", badRequest.Value?.ToString());
        Assert.Empty(service.ReceivedCalls());
    }

    [Fact]
    public async Task Get_WithInvalidLanguage_ReturnsBadRequest()
    {
        var service = Substitute.For<ILeaderboardService>();
        var controller = new LeaderboardController(service);

        var result = await controller.Get(
            sortBy: LeaderboardSortBy.TypingSpeed,
            language: (ProgrammingLanguage)999,
            difficulty: Difficulty.Hard,
            topic: null,
            limit: 10,
            cancellationToken: CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("language", badRequest.Value?.ToString());
        Assert.Empty(service.ReceivedCalls());
    }

    [Fact]
    public async Task Get_WithInvalidDifficulty_ReturnsBadRequest()
    {
        var service = Substitute.For<ILeaderboardService>();
        var controller = new LeaderboardController(service);

        var result = await controller.Get(
            sortBy: LeaderboardSortBy.TypingSpeed,
            language: ProgrammingLanguage.CSharp,
            difficulty: (Difficulty)999,
            topic: null,
            limit: 10,
            cancellationToken: CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("difficulty", badRequest.Value?.ToString());
        Assert.Empty(service.ReceivedCalls());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Get_WithOutOfBoundsLimit_ReturnsBadRequest(int limit)
    {
        var service = Substitute.For<ILeaderboardService>();
        var controller = new LeaderboardController(service);

        var result = await controller.Get(
            sortBy: LeaderboardSortBy.TypingSpeed,
            language: ProgrammingLanguage.CSharp,
            difficulty: Difficulty.Hard,
            topic: null,
            limit: limit,
            cancellationToken: CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(service.ReceivedCalls());
    }
}
