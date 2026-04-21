namespace Head.Net.AspNetCore;

/// <summary>
/// Builds paginated queries with filtering support.
/// Applies custom query filters, normalizes pagination parameters, and returns structured results.
/// Isolates list endpoint query logic for independent testing and future Phase 3+ sorting/filtering expansion.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public sealed class HeadQueryBuilderService<TEntity>
    where TEntity : class
{
    private readonly Func<IQueryable<TEntity>, IQueryable<TEntity>>? _queryFilter;
    private readonly bool _enablePaging;
    private readonly int _defaultPageSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeadQueryBuilderService{TEntity}"/> class.
    /// </summary>
    /// <param name="queryFilter">Optional custom filter applied before pagination.</param>
    /// <param name="enablePaging">Whether paging is enabled (default: true).</param>
    /// <param name="defaultPageSize">Maximum page size (default: 100).</param>
    public HeadQueryBuilderService(
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryFilter = null,
        bool enablePaging = true,
        int defaultPageSize = 100)
    {
        _queryFilter = queryFilter;
        _enablePaging = enablePaging;
        _defaultPageSize = defaultPageSize;
    }

    /// <summary>
    /// Builds a paginated query result from an enumerable of items.
    /// Applies custom filters, normalizes pagination parameters, and returns structured result.
    /// </summary>
    /// <param name="allItems">The source enumerable of items.</param>
    /// <param name="skip">The number of items to skip (validated to be >= 0).</param>
    /// <param name="take">The number of items to take (normalized to [1, defaultPageSize]).</param>
    /// <returns>A <see cref="HeadQueryBuildResult{TEntity}"/> containing paginated items and metadata.</returns>
    /// <remarks>
    /// Pagination normalization:
    /// - skip is clamped to 0 if negative
    /// - take is clamped to [1, defaultPageSize]
    ///
    /// Phase 3+ expansion:
    /// - Will support sorting (orderBy, orderByDescending)
    /// - Will support advanced filtering (filter dictionary)
    /// - Will optimize by pushing filters to database layer
    /// </remarks>
    public HeadQueryBuildResult<TEntity> BuildListQuery(
        IEnumerable<TEntity> allItems,
        int skip = 0,
        int take = 100)
    {
        // Normalize take to ensure it's within valid range
        take = Math.Min(take, _defaultPageSize);
        take = Math.Max(take, 1);

        // Apply custom filter if configured
        var filtered = _queryFilter?.Invoke(allItems.AsQueryable()) ?? allItems.AsQueryable();

        // Get total count before pagination
        var total = filtered.Count();

        // Apply pagination with safe skip normalization
        var items = filtered
            .Skip(Math.Max(0, skip))
            .Take(take)
            .ToList();

        return new HeadQueryBuildResult<TEntity>(items, total, skip, take);
    }
}
