using Head.Net.Abstractions;

namespace Head.Net.AspNetCore;

/// <summary>
/// Manages entity lifecycle hook execution with consistent error handling and cancellation support.
/// Orchestrates before/after hooks for CRUD operations in the correct sequence.
/// Eliminates duplication of hook execution logic that was spread across Create, Update, Delete endpoints.
/// Foundation for Phase 3+ features like validation pipelines, hook chaining, and audit logging.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The entity's primary key type.</typeparam>
public sealed class HeadHookExecutionService<TEntity, TKey>
    where TEntity : class
    where TKey : notnull, IEquatable<TKey>
{
    private readonly HeadBeforeCreateDelegate<TEntity>? _beforeCreate;
    private readonly HeadAfterCreateDelegate<TEntity>? _afterCreate;
    private readonly HeadBeforeUpdateDelegate<TEntity, TKey>? _beforeUpdate;
    private readonly HeadAfterUpdateDelegate<TEntity, TKey>? _afterUpdate;
    private readonly HeadBeforeDeleteDelegate<TEntity, TKey>? _beforeDelete;
    private readonly HeadAfterDeleteDelegate<TEntity>? _afterDelete;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeadHookExecutionService{TEntity, TKey}"/> class.
    /// </summary>
    /// <param name="beforeCreate">Hook executed before entity creation.</param>
    /// <param name="afterCreate">Hook executed after entity creation.</param>
    /// <param name="beforeUpdate">Hook executed before entity update.</param>
    /// <param name="afterUpdate">Hook executed after entity update.</param>
    /// <param name="beforeDelete">Hook executed before entity deletion.</param>
    /// <param name="afterDelete">Hook executed after entity deletion.</param>
    public HeadHookExecutionService(
        HeadBeforeCreateDelegate<TEntity>? beforeCreate = null,
        HeadAfterCreateDelegate<TEntity>? afterCreate = null,
        HeadBeforeUpdateDelegate<TEntity, TKey>? beforeUpdate = null,
        HeadAfterUpdateDelegate<TEntity, TKey>? afterUpdate = null,
        HeadBeforeDeleteDelegate<TEntity, TKey>? beforeDelete = null,
        HeadAfterDeleteDelegate<TEntity>? afterDelete = null)
    {
        _beforeCreate = beforeCreate;
        _afterCreate = afterCreate;
        _beforeUpdate = beforeUpdate;
        _afterUpdate = afterUpdate;
        _beforeDelete = beforeDelete;
        _afterDelete = afterDelete;
    }

    /// <summary>
    /// Executes the before-create hook if registered.
    /// Returns null to indicate success and proceed, or HeadHookResult with validation errors.
    /// </summary>
    /// <param name="entity">The entity being created.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>Null or HeadHookResult from the hook.</returns>
    public async ValueTask<HeadHookResult<TEntity>?> ExecuteBeforeCreateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        if (_beforeCreate is null)
        {
            return null; // No hook registered; proceed
        }
        return await _beforeCreate(entity, cancellationToken);
    }

    /// <summary>
    /// Executes the after-create hook if registered.
    /// </summary>
    /// <param name="entity">The entity that was created.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    public ValueTask ExecuteAfterCreateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        return _afterCreate is null
            ? default
            : _afterCreate(entity, cancellationToken);
    }

    /// <summary>
    /// Executes the before-update hook if registered.
    /// Returns null to indicate success and proceed, or HeadHookResult with validation errors.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="entity">The updated entity data.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>Null or HeadHookResult from the hook.</returns>
    public async ValueTask<HeadHookResult<TEntity>?> ExecuteBeforeUpdateAsync(TKey id, TEntity entity, CancellationToken cancellationToken)
    {
        if (_beforeUpdate is null)
        {
            return null; // No hook registered; proceed
        }
        return await _beforeUpdate(id, entity, cancellationToken);
    }

    /// <summary>
    /// Executes the after-update hook if registered.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="entity">The updated entity.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    public ValueTask ExecuteAfterUpdateAsync(TKey id, TEntity entity, CancellationToken cancellationToken)
    {
        return _afterUpdate is null
            ? default
            : _afterUpdate(id, entity, cancellationToken);
    }

    /// <summary>
    /// Executes the before-delete hook if registered.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    public ValueTask ExecuteBeforeDeleteAsync(TKey id, CancellationToken cancellationToken)
    {
        return _beforeDelete is null
            ? default
            : _beforeDelete(id, cancellationToken);
    }

    /// <summary>
    /// Executes the after-delete hook if registered.
    /// </summary>
    /// <param name="entity">The entity that was deleted.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    public ValueTask ExecuteAfterDeleteAsync(TEntity entity, CancellationToken cancellationToken)
    {
        return _afterDelete is null
            ? default
            : _afterDelete(entity, cancellationToken);
    }
}
