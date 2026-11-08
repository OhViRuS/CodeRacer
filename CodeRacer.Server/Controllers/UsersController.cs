using CodeRacer.Server.Dtos;
using CodeRacer.Server.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CodeRacer.Server.Controllers;

/// <summary>
/// API controller for user management.
/// Handles user registration, login, and profile operations.
/// Thread-safe: each request gets its own cancellation token via ASP.NET Core.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserService userService) : ControllerBase
{
    private readonly IUserService _userService = userService;

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
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

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
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _userService.LoginAsync(request, cancellationToken);
        if (result == null)
        {
            return Unauthorized(new { message = "Invalid username/email or password." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets the profile of a specific user.
    /// Returns 200 (OK) with user information,
    /// 404 (Not Found) if user doesn't exist.
    /// </summary>
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<UserDto>> GetProfile(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserProfileAsync(userId, cancellationToken);
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
        var statistics = await _userService.GetUserStatisticsAsync(userId, cancellationToken);
        if (statistics == null)
        {
            return NotFound();
        }

        return Ok(statistics);
    }
}
