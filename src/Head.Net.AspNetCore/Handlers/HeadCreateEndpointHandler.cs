using Head.Net.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Head.Net.AspNetCore;

/// <summary>
/// Maps the POST / endpoint for creating entities.
/// Executes before/after hooks via <see cref="HeadHookExecutionService{TEntity, TKey}"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The entity's primary key type.</typeparam>
internal static class HeadCreateEndpointHandler<TEntity, TKey>
    where TEntity : class, IHeadEntity<TKey>
    where TKey : notnull, IEquatable<TKey>
{
    /// <summary>
    /// Registers the create endpoint with the route group.
    /// </summary>
    /// <param name="group">The route group builder.</param>
    /// <param name="hookService">The hook execution service for lifecycle callbacks.</param>
    /// <param name="routePattern">The base route pattern for generating location header.</param>
    public static void Map(
        RouteGroupBuilder group,
        HeadHookExecutionService<TEntity, TKey> hookService,
        string routePattern)
    {
        group.MapPost("/", async (
            TEntity entity,
            IHeadEntityStore<TEntity, TKey> store,
            CancellationToken cancellationToken) =>
        {
            // Execute before-create hook; check for validation errors
            var hookResult = await hookService.ExecuteBeforeCreateAsync(entity, cancellationToken);
            if (hookResult?.ShouldProceed == false)
            {
                return HeadErrorResponseService.ValidationFailed(hookResult.ValidationResult);
            }

            var created = await store.CreateAsync(entity, cancellationToken);

            await hookService.ExecuteAfterCreateAsync(created, cancellationToken);

            await store.SaveChangesAsync(cancellationToken);
            return Results.Created($"{routePattern}/{created.Id}", created);
        }).WithName($"{typeof(TEntity).Name}_Create");
    }
}
