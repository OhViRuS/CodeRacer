using CodeRacer.Server.Data;
using CodeRacer.Server.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CodeRacer.Server.Repositories;

/// <summary>
/// Generic repository implementation using Entity Framework Core.
/// Provides base CRUD operations with async/await throughout.
/// Cancellation tokens are properly propagated to database operations.
/// </summary>
public class Repository<T>(CodeRacerDbContext context) : IRepository<T> where T : class
{
    protected readonly CodeRacerDbContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await DbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await DbSet.FindAsync(new[] { id }, cancellationToken: cancellationToken);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entry = await DbSet.AddAsync(entity, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entry.Entity;
    }

    public virtual async Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Check if entity exists by attempting to update
        var existing = await DbSet.FindAsync(
            new[] { GetEntityId(entity) },
            cancellationToken: cancellationToken);

        if (existing == null)
        {
            return false;
        }

        // FindAsync starts tracking 'existing'; attaching a second instance with the
        // same key would throw, so copy the new values onto the tracked entity instead.
        if (!ReferenceEquals(existing, entity))
        {
            Context.Entry(existing).CurrentValues.SetValues(entity);
        }

        await Context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public virtual async Task<bool> DeleteAsync(object id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existing = await DbSet.FindAsync(new[] { id }, cancellationToken: cancellationToken);
        if (existing == null)
        {
            return false;
        }

        DbSet.Remove(existing);
        await Context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Helper method to extract primary key from entity.
    /// Works with Id property (Guid or other types).
    /// </summary>
    protected static object GetEntityId(T entity)
    {
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty == null)
        {
            throw new InvalidOperationException($"Entity {typeof(T).Name} does not have an Id property");
        }

        var value = idProperty.GetValue(entity);
        return value ?? throw new InvalidOperationException($"Entity {typeof(T).Name} has null Id");
    }
}
