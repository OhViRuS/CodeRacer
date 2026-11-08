using CodeRacer.Server.Data;
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
