using Head.Net.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Head.Net.AspNetCore;

/// <summary>
/// Maps the PUT /{id} endpoint for updating entities.
/// Performs authorization checks and executes before/after hooks.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The entity's primary key type.</typeparam>
internal static class HeadUpdateEndpointHandler<TEntity, TKey>
    where TEntity : class, IHeadEntity<TKey>
    where TKey : notnull, IEquatable<TKey>
{
    /// <summary>
    /// Registers the update endpoint with the route group.
    /// </summary>
    /// <param name="group">The route group builder.</param>
    /// <param name="authService">The authorization service for ownership and policy checks.</param>
    /// <param name="hookService">The hook execution service for lifecycle callbacks.</param>
    /// <param name="userService">The user context service for extracting current user.</param>
    public static void Map(
        RouteGroupBuilder group,
        HeadAuthorizationService<TEntity, TKey> authService,
        HeadHookExecutionService<TEntity, TKey> hookService,
        HeadUserContextService userService)
    {
        group.MapPut("/{id}", async (
            TKey id,
            TEntity entity,
            IHeadEntityStore<TEntity, TKey> store,
            HttpContext ctx,
            CancellationToken cancellationToken) =>
        {
            entity.Id = id;

            var existing = await store.GetAsync(id, cancellationToken);
            if (existing is null)
            {
                return HeadErrorResponseService.NotFound(typeof(TEntity).Name, id);
            }

            var authorized = await authService.IsAuthorizedAsync(existing, ctx, cancellationToken);
            if (!authorized)
            {
                return Results.Forbid();
            }

            // Execute before-update hook; check for validation errors
            var hookResult = await hookService.ExecuteBeforeUpdateAsync(id, entity, cancellationToken);
            if (hookResult?.ShouldProceed == false)
            {
                return HeadErrorResponseService.ValidationFailed(hookResult.ValidationResult);
            }

            var updated = await store.UpdateAsync(id, entity, cancellationToken);
            if (updated is null)
            {
                return HeadErrorResponseService.NotFound(typeof(TEntity).Name, id);
            }

            await hookService.ExecuteAfterUpdateAsync(id, updated, cancellationToken);

            await store.SaveChangesAsync(cancellationToken);
            return Results.Ok(updated);
        }).WithName($"{typeof(TEntity).Name}_Update");
    }
}
