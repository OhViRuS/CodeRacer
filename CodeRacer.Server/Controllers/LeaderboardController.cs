using CodeRacer.Server.Dtos;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using CodeRacer.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeRacer.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaderboardController(ILeaderboardService leaderboardService) : ControllerBase
{
    private readonly ILeaderboardService _leaderboardService = leaderboardService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LeaderboardEntryDto>>> Get(
        [FromQuery] LeaderboardSortBy? sortBy = null,
        [FromQuery] ProgrammingLanguage? language = null,
        [FromQuery] Difficulty? difficulty = null,
        [FromQuery] string? topic = null,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (sortBy is null || language is null || difficulty is null)
        {
            return BadRequest("SortBy, language, and difficulty are required for a comparable leaderboard.");
        }

        if (!Enum.IsDefined(typeof(LeaderboardSortBy), sortBy.Value))
        {
            return BadRequest("Invalid sortBy value.");
        }

        if (!Enum.IsDefined(typeof(ProgrammingLanguage), language.Value))
        {
            return BadRequest("Invalid language value.");
        }

        if (!Enum.IsDefined(typeof(Difficulty), difficulty.Value))
        {
            return BadRequest("Invalid difficulty value.");
        }

        if (limit is < 1 or > LeaderboardService.MaxLimit)
        {
            return BadRequest($"Limit must be between 1 and {LeaderboardService.MaxLimit}.");
        }

        var query = new LeaderboardQuery(sortBy.Value, language.Value, difficulty.Value, topic, limit);
        var entries = await _leaderboardService.GetAsync(query, cancellationToken);
        return Ok(entries);
    }
}
