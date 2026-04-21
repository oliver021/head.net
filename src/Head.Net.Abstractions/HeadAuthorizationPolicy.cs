namespace Head.Net.Abstractions;

/// <summary>
/// Represents an authorization policy for entity operations.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <param name="entity">The entity being accessed.</param>
/// <param name="userId">The current user identifier.</param>
/// <param name="cancellationToken">The request cancellation token.</param>
/// <returns>True if the user is authorized for this entity, false otherwise.</returns>
public delegate ValueTask<bool> HeadAuthorizationPolicyDelegate<TEntity>(
    TEntity entity,
    int userId,
    CancellationToken cancellationToken)
    where TEntity : class;

/// <summary>
/// Represents an ownership requirement for an entity property.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <param name="entity">The entity being checked.</param>
/// <returns>The user ID that owns this entity.</returns>
public delegate int HeadOwnershipExtractor<TEntity>(TEntity entity)
    where TEntity : class;

/// <summary>
/// Encapsulates authorization context for an entity operation.
/// </summary>
public sealed class HeadAuthorizationContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeadAuthorizationContext"/> class.
    /// </summary>
    public HeadAuthorizationContext(int userId, string? role = null)
    {
        UserId = userId;
        Role = role;
    }

    /// <summary>
    /// Gets the current user identifier.
    /// </summary>
    public int UserId { get; }

    /// <summary>
    /// Gets the current user's role.
    /// </summary>
    public string? Role { get; }

    /// <summary>
    /// Gets a value indicating whether the user is authenticated.
    /// </summary>
    public bool IsAuthenticated => UserId > 0;
}

/// <summary>
/// Result of an authorization check.
/// </summary>
public sealed class HeadAuthorizationResult
{
    private HeadAuthorizationResult(bool allowed, string? reason = null)
    {
        Allowed = allowed;
        Reason = reason;
    }

    /// <summary>
    /// Gets a value indicating whether the operation is authorized.
    /// </summary>
    public bool Allowed { get; }

    /// <summary>
    /// Gets the reason for denial, if applicable.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Creates an authorized result.
    /// </summary>
    public static HeadAuthorizationResult Allow() => new(true);

    /// <summary>
    /// Creates a denied result with an optional reason.
    /// </summary>
    public static HeadAuthorizationResult Deny(string reason = "Unauthorized") => new(false, reason);
}
