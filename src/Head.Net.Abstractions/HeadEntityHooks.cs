namespace Head.Net.Abstractions;

/// <summary>
/// Represents a hook that runs before an entity is created.
/// Returns null to proceed with creation, or a validation result to reject with errors.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <param name="entity">The entity being created.</param>
/// <param name="cancellationToken">The request cancellation token.</param>
/// <returns>
/// Null or HeadHookResult with ShouldProceed=true to proceed,
/// HeadHookResult with ShouldProceed=false to reject with validation errors.
/// </returns>
public delegate ValueTask<HeadHookResult<TEntity>?> HeadBeforeCreateDelegate<TEntity>(TEntity entity, CancellationToken cancellationToken)
    where TEntity : class;

/// <summary>
/// Represents a hook that runs after an entity is created.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <param name="entity">The entity that was created.</param>
/// <param name="cancellationToken">The request cancellation token.</param>
public delegate ValueTask HeadAfterCreateDelegate<TEntity>(TEntity entity, CancellationToken cancellationToken);

/// <summary>
/// Represents a hook that runs before an entity is updated.
/// Returns null to proceed with update, or a validation result to reject with errors.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The entity's primary key type.</typeparam>
/// <param name="id">The entity identifier.</param>
/// <param name="entity">The updated entity data.</param>
/// <param name="cancellationToken">The request cancellation token.</param>
/// <returns>
/// Null or HeadHookResult with ShouldProceed=true to proceed,
/// HeadHookResult with ShouldProceed=false to reject with validation errors.
/// </returns>
public delegate ValueTask<HeadHookResult<TEntity>?> HeadBeforeUpdateDelegate<TEntity, TKey>(TKey id, TEntity entity, CancellationToken cancellationToken)
    where TEntity : class
    where TKey : notnull, IEquatable<TKey>;

/// <summary>
/// Represents a hook that runs after an entity is updated.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The entity's primary key type.</typeparam>
/// <param name="id">The entity identifier.</param>
/// <param name="entity">The updated entity.</param>
/// <param name="cancellationToken">The request cancellation token.</param>
public delegate ValueTask HeadAfterUpdateDelegate<TEntity, TKey>(TKey id, TEntity entity, CancellationToken cancellationToken)
    where TEntity : class
    where TKey : notnull, IEquatable<TKey>;

/// <summary>
/// Represents a hook that runs before an entity is deleted.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The entity's primary key type.</typeparam>
/// <param name="id">The entity identifier.</param>
/// <param name="cancellationToken">The request cancellation token.</param>
public delegate ValueTask HeadBeforeDeleteDelegate<TEntity, TKey>(TKey id, CancellationToken cancellationToken)
    where TEntity : class
    where TKey : notnull, IEquatable<TKey>;

/// <summary>
/// Represents a hook that runs after an entity is deleted.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <param name="entity">The deleted entity.</param>
/// <param name="cancellationToken">The request cancellation token.</param>
public delegate ValueTask HeadAfterDeleteDelegate<TEntity>(TEntity entity, CancellationToken cancellationToken)
    where TEntity : class;
