namespace Head.Net.Abstractions;

/// <summary>
/// Defines the minimal persistence contract for Head.Net entities.
/// Supports any key type (int, Guid, long, string, etc.) via TKey.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The entity's primary key type. Must be non-null and equatable.</typeparam>
public interface IHeadEntityStore<TEntity, TKey>
    where TEntity : class, IHeadEntity<TKey>
    where TKey : notnull, IEquatable<TKey>
{
    /// <summary>
    /// Lists all entities.
    /// </summary>
    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves an entity by identifier.
    /// </summary>
    Task<TEntity?> GetAsync(TKey id, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new entity.
    /// </summary>
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    Task<TEntity?> UpdateAsync(TKey id, TEntity entity, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an entity.
    /// </summary>
    Task<TEntity?> DeleteAsync(TKey id, CancellationToken cancellationToken);

    /// <summary>
    /// Persists pending entity changes that occurred outside the core CRUD methods.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
