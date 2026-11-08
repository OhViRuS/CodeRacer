using CodeRacer.Server.Dtos;
using CodeRacer.Server.Exceptions;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Services;

/// <summary>
/// User service implementation.
/// Coordinates user registration, authentication, and profile retrieval.
/// Uses IUserRepository for data access, IAuthenticationService for security
/// and TimeProvider for the current time.
/// </summary>
public class UserService(
    IUserRepository userRepository,
    IAuthenticationService authenticationService,
    TimeProvider timeProvider) : IUserService
{
    private const int MinimumPasswordLength = 8;

    private readonly IUserRepository _userRepository = userRepository;
    private readonly IAuthenticationService _authenticationService = authenticationService;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<AuthenticationResponse?> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Email))
        {
            return null;
        }

        // '@' is kept out of usernames so a username can never shadow another account's email at login.
        if (request.UserName.Contains('@'))
        {
            return null;
        }

        if (GuestAccount.IsReservedUserName(request.UserName) || GuestAccount.IsReservedEmail(request.Email))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.Password)
            || request.Password != request.ConfirmPassword
            || request.Password.Length < MinimumPasswordLength)
        {
            return null;
        }

        if (await _userRepository.UserNameExistsAsync(request.UserName, cancellationToken))
        {
            return null;
        }

        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            return null;
        }

        var user = new User
        {
            UserName = request.UserName,
            Email = request.Email,
            PasswordHash = _authenticationService.HashPassword(request.Password),
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        User createdUser;
        try
        {
            createdUser = await _userRepository.AddAsync(user, cancellationToken);
        }
        catch (DuplicateUserException)
        {
            // Someone registered the same username/email between the checks above and the insert.
            return null;
        }

        return CreateAuthenticationResponse(createdUser);
    }

    public async Task<AuthenticationResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.UserNameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        var identifier = request.UserNameOrEmail;
        User? user;
        if (identifier.Contains('@'))
        {
            user = await _userRepository.FindByEmailAsync(identifier, cancellationToken);
            user ??= await _userRepository.FindByUserNameAsync(identifier, cancellationToken);
        }
        else
        {
            user = await _userRepository.FindByUserNameAsync(identifier, cancellationToken);
            user ??= await _userRepository.FindByEmailAsync(identifier, cancellationToken);
        }

        if (user == null)
        {
            // Spend the same time as a real password check, so response times don't reveal
            // which usernames and emails have accounts.
            _authenticationService.SimulatePasswordCheck(request.Password);
            return null;
        }

        if (user.Id == GuestAccount.Id
            || !_authenticationService.VerifyPassword(request.Password, user.PasswordHash))
        {
            return null;
        }

        return CreateAuthenticationResponse(user);
    }

    public async Task<UserDto?> GetOwnProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user == null ? null : ToUserDto(user);
    }

    public async Task<PublicUserDto?> GetPublicProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user == null ? null : new PublicUserDto(user.Id, user.UserName, user.CreatedAt);
    }

    private AuthenticationResponse CreateAuthenticationResponse(User user)
    {
        var token = _authenticationService.GenerateToken(user.Id, user.UserName);
        return new AuthenticationResponse(token.Token, ToUserDto(user), token.ExpiresAt);
    }

    private static UserDto ToUserDto(User user) =>
        new(user.Id, user.UserName, user.Email, user.CreatedAt);
}
