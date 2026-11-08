using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CodeRacer.Server.Controllers;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CodeRacer.Server.Tests;

public class RaceResultsControllerTests
{
    private class MockRaceResultService : IRaceResultService
    {
        public CreateRaceResultRequest? LastSavedRequest { get; private set; }

        public Task<RaceResultResponse> SaveResultAsync(CreateRaceResultRequest request, CancellationToken cancellationToken = default)
        {
            LastSavedRequest = request;
            return Task.FromResult(new RaceResultResponse
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId ?? Guid.NewGuid(),
                CodeSnippetId = request.CodeSnippetId,
                TypingSpeed = request.TypingSpeed,
                Accuracy = request.Accuracy,
                Mistakes = request.Mistakes,
                CompletionTime = request.CompletionTime,
                CreatedAt = DateTime.UtcNow
            });
        }

        public Task<IReadOnlyList<RaceResultResponse>> GetHistoryAsync(Guid? userId = null, int limit = 20, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<RaceResultResponse> list = new List<RaceResultResponse>
            {
                new() { Id = Guid.NewGuid(), TypingSpeed = 75.0 }
            };
            return Task.FromResult(list);
        }

        public Task<UserStatsResponse?> GetUserStatsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var stats = new UserStatsResponse
            {
                UserId = userId,
                TotalRaces = 5,
                BestTypingSpeed = 85.0
            };
            return Task.FromResult<UserStatsResponse?>(stats);
        }
    }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedAtAction()
    {
        var mockService = new MockRaceResultService();
        var controller = new RaceResultsController(mockService);

        var request = new CreateRaceResultRequest
        {
            TypingSpeed = 70.0,
            Accuracy = 98.0,
            Mistakes = 1,
            CompletionTime = TimeSpan.FromSeconds(15)
        };

        var actionResult = await controller.Create(request, CancellationToken.None);

        var createdAtAction = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        var response = Assert.IsType<RaceResultResponse>(createdAtAction.Value);
        Assert.Equal(70.0, response.TypingSpeed);
    }

    [Fact]
    public async Task Create_WithNullRequest_ReturnsBadRequest()
    {
        var mockService = new MockRaceResultService();
        var controller = new RaceResultsController(mockService);

        var actionResult = await controller.Create(null!, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task Create_WithInvalidModelState_ReturnsBadRequest()
    {
        var mockService = new MockRaceResultService();
        var controller = new RaceResultsController(mockService);
        controller.ModelState.AddModelError("TypingSpeed", "Out of range");

        var request = new CreateRaceResultRequest();
        var actionResult = await controller.Create(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetHistory_ReturnsOkWithList()
    {
        var mockService = new MockRaceResultService();
        var controller = new RaceResultsController(mockService);

        var actionResult = await controller.GetHistory(userId: null, limit: 10);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var history = Assert.IsAssignableFrom<IReadOnlyList<RaceResultResponse>>(okResult.Value);
        Assert.Single(history);
    }

    [Fact]
    public async Task GetStats_ReturnsOkWithStats()
    {
        var mockService = new MockRaceResultService();
        var controller = new RaceResultsController(mockService);
        var userId = Guid.NewGuid();

        var actionResult = await controller.GetStats(userId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var stats = Assert.IsType<UserStatsResponse>(okResult.Value);
        Assert.Equal(userId, stats.UserId);
        Assert.Equal(5, stats.TotalRaces);
    }
}
