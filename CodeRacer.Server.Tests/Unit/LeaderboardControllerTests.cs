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
}
