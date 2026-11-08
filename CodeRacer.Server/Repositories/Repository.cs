using System.Linq.Expressions;
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
    /// Generic method: reads the entities matching <paramref name="filter"/>, sorted descending by
    /// <paramref name="orderByDescending"/> (a key of any type, e.g. a date or a score), and keeps only
    /// the first <paramref name="take"/> when it is given. The query runs in the database; results are
    /// not tracked because callers only read them.
    /// </summary>
    /// <typeparam name="TKey">Type of the sort key, inferred from <paramref name="orderByDescending"/>.</typeparam>
    protected async Task<IReadOnlyList<T>> FindOrderedDescendingAsync<TKey>(
        Expression<Func<T, bool>> filter,
        Expression<Func<T, TKey>> orderByDescending,
        int? take,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IQueryable<T> query = DbSet
            .AsNoTracking()
            .Where(filter)
            .OrderByDescending(orderByDescending);

        if (take is not null)
        {
            query = query.Take(take.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts <paramref name="entity"/> unless a row matching <paramref name="alreadyExists"/> is there.
    /// Safe under concurrent requests: if another request inserts first, the failed insert is ignored
    /// once the row is confirmed to exist. The entity is never left tracked, so a failed insert is not
    /// retried by a later SaveChanges.
    /// </summary>
    protected async Task AddIfMissingAsync(
        T entity,
        Expression<Func<T, bool>> alreadyExists,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (await DbSet.AsNoTracking().AnyAsync(alreadyExists, cancellationToken))
        {
            return;
        }

        DbSet.Add(entity);
        try
        {
            await Context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (!await DbSet.AsNoTracking().AnyAsync(alreadyExists, cancellationToken))
            {
                throw;
            }
        }
        finally
        {
            Context.Entry(entity).State = EntityState.Detached;
        }
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
