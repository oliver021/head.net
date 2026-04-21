using Head.Net.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Head.Net.AspNetCore;

/// <summary>
/// Maps the GET /{id} endpoint for retrieving a single entity.
/// Performs authorization checks via <see cref="HeadAuthorizationService{TEntity, TKey}"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The entity's primary key type.</typeparam>
internal static class HeadGetEndpointHandler<TEntity, TKey>
    where TEntity : class, IHeadEntity<TKey>
    where TKey : notnull, IEquatable<TKey>
{
    /// <summary>
    /// Registers the get endpoint with the route group.
    /// </summary>
    /// <param name="group">The route group builder.</param>
    /// <param name="authService">The authorization service for ownership and policy checks.</param>
    /// <param name="userService">The user context service for extracting current user.</param>
    public static void Map(
        RouteGroupBuilder group,
        HeadAuthorizationService<TEntity, TKey> authService,
        HeadUserContextService userService)
    {
        group.MapGet("/{id}", async (
            TKey id,
            IHeadEntityStore<TEntity, TKey> store,
            HttpContext ctx,
            CancellationToken cancellationToken) =>
        {
            var entity = await store.GetAsync(id, cancellationToken);
            if (entity is null)
            {
                return HeadErrorResponseService.NotFound(typeof(TEntity).Name, id);
            }

            var authorized = await authService.IsAuthorizedAsync(entity, ctx, cancellationToken);
            if (!authorized)
            {
                return Results.Forbid();
            }

            return Results.Ok(entity);
        }).WithName($"{typeof(TEntity).Name}_Get");
    }
}
