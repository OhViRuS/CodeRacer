using CodeRacer.Server.Data;
using CodeRacer.Server.Exceptions;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeRacer.Server.Repositories;

/// <summary>
/// User repository implementation.
/// Provides user-specific queries in addition to base CRUD operations.
/// </summary>
public class UserRepository(CodeRacerDbContext context) : Repository<User>(context), IUserRepository
{
    public override async Task<User> AddAsync(User entity, CancellationToken cancellationToken)
    {
        try
        {
            return await base.AddAsync(entity, cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            // Stop tracking the failed insert so it is not retried by a later SaveChanges.
            Context.Entry(entity).State = EntityState.Detached;

            if (await UserNameExistsAsync(entity.UserName, cancellationToken)
                || await EmailExistsAsync(entity.Email, cancellationToken))
            {
                throw new DuplicateUserException(exception);
            }

            throw;
        }
    }

    public Task EnsureGuestUserAsync(CancellationToken cancellationToken)
    {
        var guest = new User
        {
            Id = GuestAccount.Id,
            UserName = GuestAccount.UserName,
            Email = GuestAccount.Email,
            PasswordHash = GuestAccount.PasswordHash,
            CreatedAt = DateTime.UnixEpoch
        };

        return AddIfMissingAsync(guest, u => u.Id == GuestAccount.Id, cancellationToken);
    }

    public async Task<User?> FindByUserNameAsync(string userName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName.ToLower() == userName.ToLower(), cancellationToken);
    }

    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    public async Task<bool> UserNameExistsAsync(string userName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await DbSet
            .AnyAsync(u => u.UserName.ToLower() == userName.ToLower(), cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await DbSet
            .AnyAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
    }
}
