namespace Head.Net.Abstractions;

/// <summary>
/// Represents a hook that runs before an entity is created.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <param name="entity">The entity being created.</param>
/// <param name="cancellationToken">The request cancellation token.</param>
public delegate ValueTask HeadBeforeCreateDelegate<TEntity>(TEntity entity, CancellationToken cancellationToken);

/// <summary>
/// Represents a hook that runs after an entity is created.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <param name="entity">The entity that was created.</param>
/// <param name="cancellationToken">The request cancellation token.</param>
public delegate ValueTask HeadAfterCreateDelegate<TEntity>(TEntity entity, CancellationToken cancellationToken);

/// <summary>
/// Represents a hook that runs before an entity is updated.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <param name="id">The entity identifier.</param>
/// <param name="entity">The updated entity data.</param>
/// <param name="cancellationToken">The request cancellation token.</param>
public delegate ValueTask HeadBeforeUpdateDelegate<TEntity>(int id, TEntity entity, CancellationToken cancellationToken);

/// <summary>
/// Represents a hook that runs after an entity is updated.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <param name="id">The entity identifier.</param>
/// <param name="entity">The updated entity.</param>
/// <param name="cancellationToken">The request cancellation token.</param>
public delegate ValueTask HeadAfterUpdateDelegate<TEntity>(int id, TEntity entity, CancellationToken cancellationToken);

/// <summary>
/// Represents a hook that runs before an entity is deleted.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <param name="id">The entity identifier.</param>
/// <param name="cancellationToken">The request cancellation token.</param>
public delegate ValueTask HeadBeforeDeleteDelegate<TEntity>(int id, CancellationToken cancellationToken);

/// <summary>
/// Represents a hook that runs after an entity is deleted.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <param name="entity">The deleted entity.</param>
/// <param name="cancellationToken">The request cancellation token.</param>
public delegate ValueTask HeadAfterDeleteDelegate<TEntity>(TEntity entity, CancellationToken cancellationToken);
