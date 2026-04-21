using Head.Net.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Head.Net.AspNetCore;

/// <summary>
/// Maps POST /{id}/{actionName} endpoints for custom entity actions.
/// Supports domain actions like "pay", "void", "archive" with optional authorization.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The entity's primary key type.</typeparam>
internal static class HeadCustomActionEndpointHandler<TEntity, TKey>
    where TEntity : class, IHeadEntity<TKey>
    where TKey : notnull, IEquatable<TKey>
{
    /// <summary>
    /// Registers a custom action endpoint with the route group.
    /// </summary>
    /// <param name="group">The route group builder.</param>
    /// <param name="authService">The authorization service for ownership and policy checks.</param>
    /// <param name="action">The action definition (name, HTTP method, handler).</param>
    public static void Map(
        RouteGroupBuilder group,
        HeadAuthorizationService<TEntity, TKey> authService,
        HeadEntityActionDefinition<TEntity> action)
    {
        group.MapMethods("/{id}/" + action.Name, [action.HttpMethod], async (
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

            await action.Handler(entity, cancellationToken);
            await store.SaveChangesAsync(cancellationToken);
            return Results.Ok(entity);
        }).WithName($"{typeof(TEntity).Name}_{action.Name}");
    }
}
