using Head.Net.Abstractions;

namespace Head.Net.AspNetCore;

/// <summary>
/// Result of building a paginated query.
/// Contains the filtered items, total count, and pagination metadata.
/// Used by list endpoints and collection-based custom actions.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public sealed class HeadQueryBuildResult<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeadQueryBuildResult{TEntity}"/> class.
    /// </summary>
    /// <param name="items">The paginated items.</param>
    /// <param name="total">The total count of items (before pagination).</param>
    /// <param name="skip">The number of items skipped.</param>
    /// <param name="take">The number of items returned.</param>
    public HeadQueryBuildResult(List<TEntity> items, int total, int skip, int take)
    {
        Items = items;
        Total = total;
        PagedResult = new HeadPagedResult<TEntity>(items, total, skip, take);
    }

    /// <summary>
    /// Gets the paginated items.
    /// </summary>
    public List<TEntity> Items { get; }

    /// <summary>
    /// Gets the total count of items (before pagination).
    /// </summary>
    public int Total { get; }

    /// <summary>
    /// Gets the complete paged result object for HTTP responses.
    /// </summary>
    public HeadPagedResult<TEntity> PagedResult { get; }
}
