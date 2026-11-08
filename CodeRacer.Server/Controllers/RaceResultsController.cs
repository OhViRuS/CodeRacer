using System.Security.Claims;
using CodeRacer.Server.Dtos;
using CodeRacer.Server.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeRacer.Server.Controllers;

/// <summary>
/// API controller for race results.
/// Handles saving and retrieving typing challenge race results.
/// Thread-safe: each request gets its own cancellation token via ASP.NET Core.
/// All endpoints require a valid JWT bearer token.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class RaceResultsController(IRaceResultService raceResultService) : ControllerBase
{
    private readonly IRaceResultService _raceResultService = raceResultService;

    /// <summary>
    /// Gets the current authenticated user's ID from the JWT token.
    /// </summary>
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdClaim?.Value, out var userId))
        {
            return userId;
        }

        throw new UnauthorizedAccessException("User ID not found in token");
    }

    /// <summary>
    /// Saves a race result for the current user.
    /// Returns 201 (Created) with the saved race result,
    /// 401 (Unauthorized) if user is not authenticated.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RaceResultDto>> SaveRaceResult(
        [FromBody] SaveRaceResultRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _raceResultService.SaveRaceResultAsync(userId, request, cancellationToken);
            return CreatedAtAction(nameof(GetRaceResult), new { resultId = result.Id }, result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Gets a specific race result by ID.
    /// Returns 200 (OK) with the race result,
    /// 404 (Not Found) if result doesn't exist or user doesn't have access,
    /// 401 (Unauthorized) if not authenticated.
    /// </summary>
    [HttpGet("{resultId:guid}")]
    public async Task<ActionResult<RaceResultDto>> GetRaceResult(
        [FromRoute] Guid resultId,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _raceResultService.GetRaceResultByIdAsync(resultId, userId, cancellationToken);
            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
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
        try
        {
            var userId = GetCurrentUserId();
            var results = await _raceResultService.GetUserRaceResultsAsync(userId, cancellationToken);
            return Ok(results);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Gets the most recent race results for the current user.
    /// Returns 200 (OK) with list of recent race results,
    /// 401 (Unauthorized) if not authenticated.
    /// </summary>
    [HttpGet("recent")]
    public async Task<ActionResult<IReadOnlyList<RaceResultDto>>> GetRecentRaceResults(
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (count < 1 || count > 100)
            {
                return BadRequest("Count must be between 1 and 100");
            }

            var results = await _raceResultService.GetRecentRaceResultsAsync(userId, count, cancellationToken);
            return Ok(results);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Gets race results for a specific date range.
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
        try
        {
            if (from > to)
            {
                return BadRequest("'from' date must be before 'to' date");
            }

            var userId = GetCurrentUserId();
            var results = await _raceResultService.GetRaceResultsByDateRangeAsync(userId, from, to, cancellationToken);
            return Ok(results);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Deletes a race result.
    /// Returns 204 (No Content) on success,
    /// 404 (Not Found) if result doesn't exist or user doesn't have access,
    /// 401 (Unauthorized) if not authenticated.
    /// </summary>
    [HttpDelete("{resultId:guid}")]
    public async Task<IActionResult> DeleteRaceResult(
        [FromRoute] Guid resultId,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var deleted = await _raceResultService.DeleteRaceResultAsync(resultId, userId, cancellationToken);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }
}
