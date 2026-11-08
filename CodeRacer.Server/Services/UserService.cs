using CodeRacer.Server.Dtos;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Services;

/// <summary>
/// User service implementation.
/// Coordinates user registration, authentication, and profile retrieval.
/// Uses IUserRepository for data access and IAuthenticationService for security.
/// </summary>
public class UserService(
    IUserRepository userRepository,
    IRaceResultRepository raceResultRepository,
    IAuthenticationService authenticationService) : IUserService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IRaceResultRepository _raceResultRepository = raceResultRepository;
    private readonly IAuthenticationService _authenticationService = authenticationService;

    public async Task<AuthenticationResponse?> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password != request.ConfirmPassword)
        {
            return null;
        }

        if (request.Password.Length < 8)
        {
            return null;
        }

        // Check for existing username/email
        var userNameExists = await _userRepository.UserNameExistsAsync(request.UserName, cancellationToken);
        if (userNameExists)
        {
            return null;
        }

        var emailExists = await _userRepository.EmailExistsAsync(request.Email, cancellationToken);
        if (emailExists)
        {
            return null;
        }

        // Create new user
        var user = new User
        {
            UserName = request.UserName,
            Email = request.Email,
            PasswordHash = _authenticationService.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        var createdUser = await _userRepository.AddAsync(user, cancellationToken);

        // Generate token and return authentication response
        var token = _authenticationService.GenerateToken(createdUser.Id, createdUser.UserName);
        var expiresAt = DateTime.UtcNow.Add(_authenticationService.GetTokenExpiration());

        return new AuthenticationResponse(
            token,
            new UserDto(createdUser.Id, createdUser.UserName, createdUser.Email, createdUser.CreatedAt),
            expiresAt
        );
    }

    public async Task<AuthenticationResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserNameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        // Find user by username or email
        var user = await _userRepository.FindByUserNameAsync(request.UserNameOrEmail, cancellationToken);
        user ??= await _userRepository.FindByEmailAsync(request.UserNameOrEmail, cancellationToken);

        if (user == null)
        {
            return null;
        }

        // Verify password
        if (!_authenticationService.VerifyPassword(request.Password, user.PasswordHash))
        {
            return null;
        }

        // Generate token and return authentication response
        var token = _authenticationService.GenerateToken(user.Id, user.UserName);
        var expiresAt = DateTime.UtcNow.Add(_authenticationService.GetTokenExpiration());

        return new AuthenticationResponse(
            token,
            new UserDto(user.Id, user.UserName, user.Email, user.CreatedAt),
            expiresAt
        );
    }

    public async Task<UserDto?> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return null;
        }

        return new UserDto(user.Id, user.UserName, user.Email, user.CreatedAt);
    }

    public async Task<UserStatisticsDto?> GetUserStatisticsAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Check if user exists
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return null;
        }

        // Get all race results for the user
        var raceResults = await _raceResultRepository.GetByUserIdAsync(userId, cancellationToken);

        if (raceResults.Count == 0)
        {
            // User has no races yet
            return new UserStatisticsDto(
                userId,
                0,
                0.0,
                0.0,
                0.0,
                TimeSpan.Zero,
                0,
                DateTime.UtcNow,
                DateTime.UtcNow
            );
        }

        return new UserStatisticsDto(
            userId,
            raceResults.Count,
            raceResults.Average(r => r.TypingSpeed),
            raceResults.Average(r => r.Accuracy),
            raceResults.Max(r => r.TypingSpeed),
            raceResults.Min(r => r.CompletionTime),
            raceResults.Sum(r => r.Mistakes),
            raceResults.Min(r => r.CreatedAt),
            raceResults.Max(r => r.CreatedAt)
        );
    }
}
