using CodeRacer.Server.Dtos;
using CodeRacer.Server.Extensions;
using CodeRacer.Server.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace CodeRacer.Server.Controllers;

/// <summary>
/// API controller for race results.
/// Handles saving and retrieving typing challenge race results.
/// Saving is open to everyone: signed-in players save to their own account, players who are not
/// signed in save to the shared guest account. Reading and deleting require a valid JWT bearer token.
/// Request bodies are validated by [ApiController].
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RaceResultsController(IRaceResultService raceResultService) : ControllerBase
{
    private const int DefaultRecentCount = 10;
    private const int MaxRecentCount = 100;

    private readonly IRaceResultService _raceResultService = raceResultService;

    /// <summary>
    /// Saves a race result. The user always comes from the JWT, never from the request body.
    /// Without an Authorization header the result is saved to the shared guest account.
    /// Returns 201 (Created) with the saved race result,
    /// 400 (Bad Request) if the request is invalid or the code snippet does not exist,
    /// 401 (Unauthorized) if a token was sent but is invalid or expired
    /// (so a signed-in player's result is never silently saved as a guest).
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<RaceResultDto>> SaveRaceResult(
        [FromBody] SaveRaceResultRequest request,
        CancellationToken cancellationToken)
    {
        Guid? userId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            if (!User.TryGetUserId(out var signedInUserId))
            {
                return Unauthorized();
            }

            userId = signedInUserId;
        }
        else if (Request.Headers.ContainsKey(HeaderNames.Authorization))
        {
            return Unauthorized();
        }

        var result = await _raceResultService.SaveRaceResultAsync(userId, request, cancellationToken);
        if (result == null)
        {
            return BadRequest(new { message = $"Code snippet '{request.CodeSnippetId}' does not exist." });
        }

        return CreatedAtAction(nameof(GetRaceResult), new { resultId = result.Id }, result);
    }

    /// <summary>
    /// Gets a specific race result by ID.
    /// Returns 200 (OK) with the race result,
    /// 404 (Not Found) if result doesn't exist or belongs to another user,
    /// 401 (Unauthorized) if not authenticated.
    /// </summary>
    [HttpGet("{resultId:guid}")]
    public async Task<ActionResult<RaceResultDto>> GetRaceResult(
        [FromRoute] Guid resultId,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _raceResultService.GetRaceResultByIdAsync(resultId, userId, cancellationToken);
        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets all race results for the current user.
    /// Returns 200 (OK) with list of race results ordered by most recent first,
    /// 401 (Unauthorized) if not authenticated.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RaceResultDto>>> GetUserRaceResults(
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var results = await _raceResultService.GetUserRaceResultsAsync(userId, cancellationToken);
        return Ok(results);
    }

    /// <summary>
    /// Gets the most recent race results for the current user (10 when count is not given).
    /// Returns 200 (OK) with list of recent race results,
    /// 400 (Bad Request) if count is outside 1-100,
    /// 401 (Unauthorized) if not authenticated.
    /// </summary>
    [HttpGet("recent")]
    public async Task<ActionResult<IReadOnlyList<RaceResultDto>>> GetRecentRaceResults(
        [FromQuery] int? count,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var take = count ?? DefaultRecentCount;
        if (take < 1 || take > MaxRecentCount)
        {
            return BadRequest(new { message = $"Count must be between 1 and {MaxRecentCount}." });
        }

        var results = await _raceResultService.GetRecentRaceResultsAsync(userId, take, cancellationToken);
        return Ok(results);
    }

    /// <summary>
    /// Gets race results for a specific date range. Dates without a time zone are treated as UTC.
    /// Returns 200 (OK) with list of race results,
    /// 400 (Bad Request) if date range is invalid,
    /// 401 (Unauthorized) if not authenticated.
    /// </summary>
    [HttpGet("by-date-range")]
    public async Task<ActionResult<IReadOnlyList<RaceResultDto>>> GetRaceResultsByDateRange(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var fromUtc = from.AsUtc();
        var toUtc = to.AsUtc();
        if (fromUtc > toUtc)
        {
            return BadRequest(new { message = "'from' date must be before 'to' date." });
        }

        var results = await _raceResultService.GetRaceResultsByDateRangeAsync(userId, fromUtc, toUtc, cancellationToken);
        return Ok(results);
    }

    /// <summary>
    /// Deletes a race result.
    /// Returns 204 (No Content) on success,
    /// 404 (Not Found) if result doesn't exist or belongs to another user,
    /// 401 (Unauthorized) if not authenticated.
    /// </summary>
    [HttpDelete("{resultId:guid}")]
    public async Task<IActionResult> DeleteRaceResult(
        [FromRoute] Guid resultId,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var deleted = await _raceResultService.DeleteRaceResultAsync(resultId, userId, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
