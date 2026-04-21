namespace Head.Net.Abstractions;

/// <summary>
/// Defines the minimal persistence contract required by the first Head.Net vertical slice.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public interface IHeadEntityStore<TEntity>
    where TEntity : class, IHeadEntity<int>
{
    /// <summary>
    /// Lists all entities.
    /// </summary>
    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves an entity by identifier.
    /// </summary>
    Task<TEntity?> GetAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new entity.
    /// </summary>
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    Task<TEntity?> UpdateAsync(int id, TEntity entity, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an entity.
    /// </summary>
    Task<TEntity?> DeleteAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Persists pending entity changes that occurred outside the core CRUD methods.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
