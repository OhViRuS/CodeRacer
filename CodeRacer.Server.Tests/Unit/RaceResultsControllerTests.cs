using CodeRacer.Server.Controllers;
using CodeRacer.Server.Dtos;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Tests.TestDoubles;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace CodeRacer.Server.Tests.Unit;

/// <summary>
/// Controller tests for RaceResultsController.
/// The controller is called directly; IRaceResultService is an NSubstitute double, used as a
/// stub (to choose the case) and as a mock (to check how the service was called).
/// These tests can't see routing, [Authorize], JSON or model validation - the integration tests cover those.
/// </summary>
public class RaceResultsControllerTests
{
    private readonly IRaceResultService _service = Substitute.For<IRaceResultService>();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly RaceResultsController _controller;

    public RaceResultsControllerTests()
    {
        _controller = new RaceResultsController(_service).SignedInAs(_userId);
    }

    private static SaveRaceResultRequest CreateRequest() =>
        new(Guid.NewGuid(), TimeSpan.FromSeconds(30), 100, 95, 2);

    private RaceResultDto CreateDto(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), _userId, Guid.NewGuid(), TimeSpan.FromSeconds(30), 100, 95, 2, FixedTimeProvider.DefaultNow.UtcDateTime);

    [Fact]
    public async Task SaveRaceResult_ValidRequest_ReturnsCreatedAtGetRaceResult()
    {
        // Arrange
        var request = CreateRequest();
        var dto = CreateDto();
        _service.SaveRaceResultAsync(_userId, request, Arg.Any<CancellationToken>()).Returns(dto);

        // Act
        var response = await _controller.SaveRaceResult(request, CancellationToken.None);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(response.Result);
        Assert.Equal(nameof(RaceResultsController.GetRaceResult), created.ActionName);
        Assert.Equal(dto.Id, created.RouteValues!["resultId"]);
        Assert.Same(dto, created.Value);
    }

    [Fact]
    public async Task SaveRaceResult_ValidRequest_PassesRequestTokenToService()
    {
        // Arrange
        var request = CreateRequest();
        using var cts = new CancellationTokenSource();
        _service.SaveRaceResultAsync(_userId, request, cts.Token).Returns(CreateDto());

        // Act
        await _controller.SaveRaceResult(request, cts.Token);

        // Assert - the request's token, not a new or default one, reaches the service
        await _service.Received(1).SaveRaceResultAsync(_userId, request, cts.Token);
    }

    [Fact]
    public async Task SaveRaceResult_UnknownSnippet_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateRequest();
        _service.SaveRaceResultAsync(_userId, request, Arg.Any<CancellationToken>()).Returns((RaceResultDto?)null);

        // Act
        var response = await _controller.SaveRaceResult(request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    [Fact]
    public async Task SaveRaceResult_NotSignedIn_SavesAsGuest()
    {
        // Arrange
        var controller = new RaceResultsController(_service).SignedOut();
        var request = CreateRequest();
        _service.SaveRaceResultAsync(null, request, Arg.Any<CancellationToken>()).Returns(CreateDto());

        // Act
        var response = await controller.SaveRaceResult(request, CancellationToken.None);

        // Assert - no user id is passed, so the service uses the guest account
        Assert.IsType<CreatedAtActionResult>(response.Result);
        await _service.Received(1).SaveRaceResultAsync(null, request, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveRaceResult_RejectedToken_ReturnsUnauthorizedWithoutSavingAsGuest()
    {
        // Arrange - a signed-in player whose token expired must not end up saving as a guest
        var controller = new RaceResultsController(_service).WithRejectedToken();

        // Act
        var response = await controller.SaveRaceResult(CreateRequest(), CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedResult>(response.Result);
        Assert.Empty(_service.ReceivedCalls());
    }

    [Fact]
    public async Task GetRaceResult_Exists_ReturnsOk()
    {
        // Arrange
        var dto = CreateDto();
        _service.GetRaceResultByIdAsync(dto.Id, _userId, Arg.Any<CancellationToken>()).Returns(dto);

        // Act
        var response = await _controller.GetRaceResult(dto.Id, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(response.Result);
        Assert.Same(dto, ok.Value);
    }

    [Fact]
    public async Task GetRaceResult_NotFoundOrNotOwned_ReturnsNotFound()
    {
        // Arrange
        var resultId = Guid.NewGuid();
        _service.GetRaceResultByIdAsync(resultId, _userId, Arg.Any<CancellationToken>()).Returns((RaceResultDto?)null);

        // Act
        var response = await _controller.GetRaceResult(resultId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(response.Result);
    }

    [Fact]
    public async Task GetUserRaceResults_SignedIn_ReturnsOwnResultsAndPassesToken()
    {
        // Arrange
        IReadOnlyList<RaceResultDto> results = [CreateDto(), CreateDto()];
        using var cts = new CancellationTokenSource();
        _service.GetUserRaceResultsAsync(_userId, cts.Token).Returns(results);

        // Act
        var response = await _controller.GetUserRaceResults(cts.Token);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(response.Result);
        Assert.Same(results, ok.Value);
        await _service.Received(1).GetUserRaceResultsAsync(_userId, cts.Token);
    }

    [Fact]
    public async Task GetRecentRaceResults_NoCount_AsksServiceForTen()
    {
        // Arrange
        _service.GetRecentRaceResultsAsync(_userId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);

        // Act
        await _controller.GetRecentRaceResults(null, CancellationToken.None);

        // Assert
        await _service.Received(1).GetRecentRaceResultsAsync(_userId, 10, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task GetRecentRaceResults_CountOutOfRange_ReturnsBadRequestWithoutCallingService(int count)
    {
        // Act
        var response = await _controller.GetRecentRaceResults(count, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.Empty(_service.ReceivedCalls());
    }

    [Fact]
    public async Task GetRaceResultsByDateRange_FromAfterTo_ReturnsBadRequestWithoutCallingService()
    {
        // Act
        var response = await _controller.GetRaceResultsByDateRange(
            new DateTime(2026, 2, 1), new DateTime(2026, 1, 1), CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.Empty(_service.ReceivedCalls());
    }

    [Fact]
    public async Task GetRaceResultsByDateRange_DatesWithoutTimeZone_PassesUtcDatesToService()
    {
        // Arrange - query-string dates arrive with DateTimeKind.Unspecified, which PostgreSQL rejects
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 2, 1);
        _service.GetRaceResultsByDateRangeAsync(_userId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var response = await _controller.GetRaceResultsByDateRange(from, to, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(response.Result);
        await _service.Received(1).GetRaceResultsByDateRangeAsync(
            _userId,
            Arg.Is<DateTime>(d => d == from && d.Kind == DateTimeKind.Utc),
            Arg.Is<DateTime>(d => d == to && d.Kind == DateTimeKind.Utc),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteRaceResult_OwnResult_ReturnsNoContent()
    {
        // Arrange
        var resultId = Guid.NewGuid();
        _service.DeleteRaceResultAsync(resultId, _userId, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var response = await _controller.DeleteRaceResult(resultId, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(response);
    }

    [Fact]
    public async Task DeleteRaceResult_NotFoundOrNotOwned_ReturnsNotFound()
    {
        // Arrange
        var resultId = Guid.NewGuid();
        _service.DeleteRaceResultAsync(resultId, _userId, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var response = await _controller.DeleteRaceResult(resultId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(response);
    }
}
