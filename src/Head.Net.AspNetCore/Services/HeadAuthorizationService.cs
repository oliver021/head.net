using Head.Net.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Head.Net.AspNetCore;

/// <summary>
/// Encapsulates authorization logic for entity CRUD operations.
/// Checks custom authorization policies, ownership rules, and provides unified access control.
/// Eliminates duplication that was previously spread across GET, UPDATE, DELETE endpoints.
/// Extension point for Phase 3+ role-based authorization and detailed denial reasons.
/// </summary>
/// <typeparam name="TEntity">The entity type being authorized.</typeparam>
/// <typeparam name="TKey">The entity's primary key type.</typeparam>
public sealed class HeadAuthorizationService<TEntity, TKey>
    where TEntity : class
    where TKey : notnull, IEquatable<TKey>
{
    private readonly HeadAuthorizationPolicyDelegate<TEntity>? _authorizationPolicy;
    private readonly HeadOwnershipExtractor<TEntity>? _ownershipExtractor;
    private readonly HeadUserContextService _userContextService;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeadAuthorizationService{TEntity, TKey}"/> class.
    /// </summary>
    /// <param name="authorizationPolicy">Optional custom authorization policy. Checked first if provided.</param>
    /// <param name="ownershipExtractor">Optional ownership check. Checked after policy if policy is null.</param>
    /// <param name="userIdProvider">Optional custom user ID extraction. Defaults to "UserId" claim.</param>
    public HeadAuthorizationService(
        HeadAuthorizationPolicyDelegate<TEntity>? authorizationPolicy = null,
        HeadOwnershipExtractor<TEntity>? ownershipExtractor = null,
        Func<HttpContext, int>? userIdProvider = null)
    {
        _authorizationPolicy = authorizationPolicy;
        _ownershipExtractor = ownershipExtractor;
        _userContextService = new HeadUserContextService(userIdProvider);
    }

    /// <summary>
    /// Determines if the current user is authorized to access the specified entity.
    /// Checks in order: custom authorization policy → ownership → default allow.
    /// </summary>
    /// <param name="entity">The entity being accessed.</param>
    /// <param name="context">The HTTP context.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>True if authorized, false otherwise.</returns>
    /// <remarks>
    /// Authorization precedence:
    /// 1. Custom policy (if configured) — most specific
    /// 2. Ownership check (if configured) — entity owner must match current user
    /// 3. Default allow — if neither policy nor ownership is configured
    ///
    /// Phase 3+ expansion: Will support role-based checks, delegation rules, and detailed denial reasons.
    /// </remarks>
    public async ValueTask<bool> IsAuthorizedAsync(
        TEntity entity,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        // Custom authorization policy is most specific — check first
        if (_authorizationPolicy is not null)
        {
            var userId = _userContextService.GetUserId(context);
            return await _authorizationPolicy(entity, userId, cancellationToken);
        }

        // Ownership-based authorization — user must own the entity
        if (_ownershipExtractor is not null)
        {
            var userId = _userContextService.GetUserId(context);
            var ownerId = _ownershipExtractor(entity);
            return userId == ownerId;
        }

        // No authorization configured — allow by default
        return true;
    }
}
