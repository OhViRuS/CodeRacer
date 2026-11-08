using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CodeRacer.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RaceResultsController : ControllerBase
{
    private readonly IRaceResultService _raceResultService;

    public RaceResultsController(IRaceResultService raceResultService)
    {
        _raceResultService = raceResultService ?? throw new ArgumentNullException(nameof(raceResultService));
    }

    // POST: /api/raceresults
    [HttpPost]
    [ProducesResponseType(typeof(RaceResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RaceResultResponse>> Create(
        [FromBody] CreateRaceResultRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(new { message = "Request body cannot be null." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        RaceResultResponse saved = await _raceResultService.SaveResultAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetHistory), new { userId = saved.UserId }, saved);
    }

    // GET: /api/raceresults/history?userId={id}&limit=20
    [HttpGet("history")]
    [ProducesResponseType(typeof(IReadOnlyList<RaceResultResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RaceResultResponse>>> GetHistory(
        [FromQuery] Guid? userId = null,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<RaceResultResponse> history = await _raceResultService.GetHistoryAsync(userId, limit, cancellationToken);
        return Ok(history);
    }

    // GET: /api/raceresults/stats/{userId}
    [HttpGet("stats/{userId:guid}")]
    [ProducesResponseType(typeof(UserStatsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserStatsResponse>> GetStats(
        Guid userId,
        CancellationToken cancellationToken)
    {
        UserStatsResponse? stats = await _raceResultService.GetUserStatsAsync(userId, cancellationToken);
        return Ok(stats);
    }
}
