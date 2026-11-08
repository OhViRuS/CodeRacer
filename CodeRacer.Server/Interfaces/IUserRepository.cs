using CodeRacer.Server.Models;

namespace CodeRacer.Server.Interfaces;

/// <summary>
/// Repository for User entity.
/// Extends IRepository with user-specific operations like FindByUserNameAsync and FindByEmailAsync.
/// AddAsync throws <see cref="CodeRacer.Server.Exceptions.DuplicateUserException"/> when the
/// username or email is already taken (the database's unique indexes are the final check).
/// Following Single Responsibility Principle - each repository handles one aggregate root.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>Finds a user by username (case-insensitive). Returns null if not found.</summary>
    Task<User?> FindByUserNameAsync(string userName, CancellationToken cancellationToken);

    /// <summary>Finds a user by email (case-insensitive). Returns null if not found.</summary>
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>Checks if a username already exists (case-insensitive).</summary>
    Task<bool> UserNameExistsAsync(string userName, CancellationToken cancellationToken);

    /// <summary>Checks if an email already exists (case-insensitive).</summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);
}
