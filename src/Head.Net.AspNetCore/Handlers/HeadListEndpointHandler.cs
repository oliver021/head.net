using Head.Net.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Head.Net.AspNetCore;

/// <summary>
/// Maps the GET / endpoint for listing entities with pagination and filtering.
/// Delegates query building to <see cref="HeadQueryBuilderService{TEntity}"/> for independent testing.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The entity's primary key type.</typeparam>
internal static class HeadListEndpointHandler<TEntity, TKey>
    where TEntity : class, IHeadEntity<TKey>
    where TKey : notnull, IEquatable<TKey>
{
    /// <summary>
    /// Registers the list endpoint with the route group.
    /// </summary>
    /// <param name="group">The route group builder.</param>
    /// <param name="queryService">The query builder service for pagination and filtering.</param>
    public static void Map(RouteGroupBuilder group, HeadQueryBuilderService<TEntity> queryService)
    {
        group.MapGet("/", async (IHeadEntityStore<TEntity, TKey> store, int skip = 0, int take = 100) =>
        {
            var allItems = await store.ListAsync(default);
            var queryResult = queryService.BuildListQuery(allItems, skip, take);
            return Results.Ok(queryResult.PagedResult);
        }).WithName($"{typeof(TEntity).Name}_List");
    }
}
