using CodeRacer.Server.Dtos;
using CodeRacer.Server.Extensions;
using CodeRacer.Server.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeRacer.Server.Controllers;

/// <summary>
/// API controller for user accounts.
/// Handles registration, login, profiles and statistics.
/// Request bodies are validated by [ApiController], which returns 400 with field errors.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController(
    IUserService userService,
    IUserStatisticsService userStatisticsService) : ControllerBase
{
    private readonly IUserService _userService = userService;
    private readonly IUserStatisticsService _userStatisticsService = userStatisticsService;

    /// <summary>
    /// Registers a new user account.
    /// Returns 201 (Created) with authentication token on success,
    /// 400 (Bad Request) if validation fails or user already exists.
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthenticationResponse>> Register(
        [FromBody] RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.RegisterAsync(request, cancellationToken);
        if (result == null)
        {
            return BadRequest(new { message = "Registration failed. Username or email may already be in use, or password requirements not met." });
        }

        return CreatedAtAction(nameof(GetProfile), new { userId = result.User.Id }, result);
    }

    /// <summary>
    /// Authenticates a user with username/email and password.
    /// Returns 200 (OK) with authentication token on success,
    /// 401 (Unauthorized) if credentials are invalid.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthenticationResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.LoginAsync(request, cancellationToken);
        if (result == null)
        {
            return Unauthorized(new { message = "Invalid username/email or password." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets the signed-in user's own profile, including the email address.
    /// Returns 200 (OK) with user information,
    /// 401 (Unauthorized) if not authenticated,
    /// 404 (Not Found) if the account no longer exists.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetOwnProfile(CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var user = await _userService.GetOwnProfileAsync(userId, cancellationToken);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    /// <summary>
    /// Gets the public profile of a specific user (no email address).
    /// Returns 200 (OK) with user information,
    /// 404 (Not Found) if user doesn't exist.
    /// </summary>
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<PublicUserDto>> GetProfile(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await _userService.GetPublicProfileAsync(userId, cancellationToken);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    /// <summary>
    /// Gets statistics for a specific user (races completed, average speed, best time, etc).
    /// Returns 200 (OK) with user statistics,
    /// 404 (Not Found) if user doesn't exist.
    /// </summary>
    [HttpGet("{userId:guid}/statistics")]
    public async Task<ActionResult<UserStatisticsDto>> GetStatistics(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var statistics = await _userStatisticsService.GetUserStatisticsAsync(userId, cancellationToken);
        if (statistics == null)
        {
            return NotFound();
        }

        return Ok(statistics);
    }
}
