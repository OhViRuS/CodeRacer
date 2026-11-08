namespace CodeRacer.Server.Interfaces;

/// <summary>
/// Generic repository interface defining CRUD operations.
/// All methods are async and require a CancellationToken (never default).
/// Following the repository pattern for data abstraction and SOLID Dependency Inversion Principle.
/// </summary>
public interface IRepository<T> where T : class
{
    /// <summary>Gets all entities asynchronously.</summary>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Gets an entity by its primary key asynchronously.</summary>
    Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken);

    /// <summary>Adds a new entity and returns the added entity with assigned Id.</summary>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken);

    /// <summary>Updates an existing entity. Returns false if entity not found.</summary>
    Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken);

    /// <summary>Deletes an entity by id. Returns false if entity not found.</summary>
    Task<bool> DeleteAsync(object id, CancellationToken cancellationToken);
}
