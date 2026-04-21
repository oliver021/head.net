using System.Linq.Expressions;
using Head.Net.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Head.Net.AspNetCore;

/// <summary>
/// Builds a minimal API surface for an entity type.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public sealed class HeadEntityEndpointBuilder<TEntity>
    where TEntity : class, IHeadEntity<int>
{
    private readonly IEndpointRouteBuilder endpoints;
    private readonly string routePattern;
    private readonly HeadCrudOptions crudOptions = new();
    private readonly List<HeadEntityActionDefinition<TEntity>> actions = [];
    private HeadBeforeCreateDelegate<TEntity>? beforeCreate;
    private HeadAfterCreateDelegate<TEntity>? afterCreate;
    private HeadBeforeUpdateDelegate<TEntity>? beforeUpdate;
    private HeadAfterUpdateDelegate<TEntity>? afterUpdate;
    private HeadBeforeDeleteDelegate<TEntity>? beforeDelete;
    private HeadAfterDeleteDelegate<TEntity>? afterDelete;
    private bool enablePaging = true;
    private int defaultPageSize = 100;
    private Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryFilter;
    private HeadOwnershipExtractor<TEntity>? ownershipExtractor;
    private HeadAuthorizationPolicyDelegate<TEntity>? authorizationPolicy;
    private Func<HttpContext, int>? userIdProvider;

    internal HeadEntityEndpointBuilder(IEndpointRouteBuilder endpoints, string routePattern)
    {
        this.endpoints = endpoints;
        this.routePattern = routePattern;
    }

    /// <summary>
    /// Configures which CRUD operations are enabled.
    /// </summary>
    public HeadEntityEndpointBuilder<TEntity> WithCrud(Action<HeadCrudOptions>? configure = null)
    {
        configure?.Invoke(crudOptions);
        return this;
    }

    /// <summary>
    /// Registers a hook that runs before an entity is created.
    /// </summary>
    public HeadEntityEndpointBuilder<TEntity> BeforeCreate(HeadBeforeCreateDelegate<TEntity> hook)
    {
        beforeCreate = hook;
        return this;
    }

    /// <summary>
    /// Registers a hook that runs after an entity is created.
    /// </summary>
    public HeadEntityEndpointBuilder<TEntity> AfterCreate(HeadAfterCreateDelegate<TEntity> hook)
    {
        afterCreate = hook;
        return this;
    }

    /// <summary>
    /// Registers a hook that runs before an entity is updated.
    /// </summary>
    public HeadEntityEndpointBuilder<TEntity> BeforeUpdate(HeadBeforeUpdateDelegate<TEntity> hook)
    {
        beforeUpdate = hook;
        return this;
    }

    /// <summary>
    /// Registers a hook that runs after an entity is updated.
    /// </summary>
    public HeadEntityEndpointBuilder<TEntity> AfterUpdate(HeadAfterUpdateDelegate<TEntity> hook)
    {
        afterUpdate = hook;
        return this;
    }

    /// <summary>
    /// Registers a hook that runs before an entity is deleted.
    /// </summary>
    public HeadEntityEndpointBuilder<TEntity> BeforeDelete(HeadBeforeDeleteDelegate<TEntity> hook)
    {
        beforeDelete = hook;
        return this;
    }

    /// <summary>
    /// Registers a hook that runs after an entity is deleted.
    /// </summary>
    public HeadEntityEndpointBuilder<TEntity> AfterDelete(HeadAfterDeleteDelegate<TEntity> hook)
    {
        afterDelete = hook;
        return this;
    }

    /// <summary>
    /// Configures list endpoint paging behavior.
    /// </summary>
    public HeadEntityEndpointBuilder<TEntity> WithPaging(bool enable = true, int defaultPageSize = 100)
    {
        enablePaging = enable;
        this.defaultPageSize = defaultPageSize;
        return this;
    }

    /// <summary>
    /// Registers a named entity action that maps to <c>/{id}/{action}</c>.
    /// </summary>
    public HeadEntityEndpointBuilder<TEntity> CustomAction(
        string name,
        Func<TEntity, CancellationToken, Task> handler,
        string httpMethod = "POST")
    {
        actions.Add(new HeadEntityActionDefinition<TEntity>(name, httpMethod, handler));
        return this;
    }

    /// <summary>
    /// Registers a custom query filter to apply to list operations.
    /// </summary>
    public HeadEntityEndpointBuilder<TEntity> WithQueryFilter(Func<IQueryable<TEntity>, IQueryable<TEntity>> filter)
    {
        queryFilter = filter;
        return this;
    }

    /// <summary>
    /// Registers an ownership check that restricts access to entities owned by the current user.
    /// </summary>
    public HeadEntityEndpointBuilder<TEntity> RequireOwnership(HeadOwnershipExtractor<TEntity> ownershipExtractor)
    {
        this.ownershipExtractor = ownershipExtractor;
        return this;
    }

    /// <summary>
    /// Registers a custom authorization policy.
    /// </summary>
    public HeadEntityEndpointBuilder<TEntity> RequireAuthorization(HeadAuthorizationPolicyDelegate<TEntity> policy)
    {
        authorizationPolicy = policy;
        return this;
    }

    /// <summary>
    /// Configures how to extract the current user ID from the HTTP context.
    /// Default: parses the "UserId" claim from the authentication principal.
    /// </summary>
    public HeadEntityEndpointBuilder<TEntity> WithUserIdProvider(Func<HttpContext, int> provider)
    {
        userIdProvider = provider;
        return this;
    }

    /// <summary>
    /// Applies configuration from a dedicated setup class, following the same
    /// pattern as EF Core's <c>ApplyConfiguration</c>.
    /// <typeparamref name="TSetup"/> is resolved via
    /// <see cref="ActivatorUtilities.CreateInstance{T}"/> — constructor parameters
    /// are injected from the application's service provider without requiring
    /// <typeparamref name="TSetup"/> to be registered in DI.
    /// </summary>
    /// <typeparam name="TSetup">A class implementing <see cref="IHeadEntitySetup{TEntity}"/>.</typeparam>
    public HeadEntityEndpointBuilder<TEntity> Setup<TSetup>()
        where TSetup : class, IHeadEntitySetup<TEntity>
    {
        var instance = ActivatorUtilities.CreateInstance<TSetup>(endpoints.ServiceProvider);
        instance.Configure(this);
        return this;
    }

    /// <summary>
    /// Maps the configured endpoints and returns the route group.
    /// </summary>
    public RouteGroupBuilder Build()
    {
        var group = endpoints.MapGroup(routePattern)
            .WithTags(typeof(TEntity).Name);

        var defaultUserIdProvider = userIdProvider ?? (ctx =>
        {
            var userIdClaim = ctx.User?.FindFirst("UserId")?.Value ?? "0";
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        });

        var checkAuthorization = async Task<bool>(TEntity entity, HttpContext ctx, CancellationToken ct) =>
        {
            if (authorizationPolicy is not null)
            {
                var userId = defaultUserIdProvider(ctx);
                return await authorizationPolicy(entity, userId, ct);
            }

            if (ownershipExtractor is not null)
            {
                var userId = defaultUserIdProvider(ctx);
                var ownerId = ownershipExtractor(entity);
                return userId == ownerId;
            }

            return true;
        };

        if (crudOptions.EnableList)
        {
            group.MapGet("/", async (IHeadEntityStore<TEntity> store, int skip = 0, int take = 100, CancellationToken cancellationToken = default) =>
            {
                take = Math.Min(take, defaultPageSize);
                take = Math.Max(take, 1);

                var allItems = await store.ListAsync(cancellationToken);
                var filtered = queryFilter?.Invoke(allItems.AsQueryable()) ?? allItems.AsQueryable();
                var total = filtered.Count();

                var items = filtered
                    .Skip(Math.Max(0, skip))
                    .Take(take)
                    .ToList();

                var result = new HeadPagedResult<TEntity>(items, total, skip, take);
                return Results.Ok(result);
            }).WithName($"{typeof(TEntity).Name}_List");
        }

        if (crudOptions.EnableGet)
        {
            group.MapGet("/{id:int}", async (int id, IHeadEntityStore<TEntity> store, HttpContext ctx, CancellationToken cancellationToken) =>
            {
                var entity = await store.GetAsync(id, cancellationToken);
                if (entity is null)
                {
                    return Results.NotFound();
                }

                if (ownershipExtractor is not null || authorizationPolicy is not null)
                {
                    var authorized = await checkAuthorization(entity, ctx, cancellationToken);
                    if (!authorized)
                    {
                        return Results.Forbid();
                    }
                }

                return Results.Ok(entity);
            }).WithName($"{typeof(TEntity).Name}_Get");
        }

        if (crudOptions.EnableCreate)
        {
            group.MapPost("/", async (TEntity entity, IHeadEntityStore<TEntity> store, CancellationToken cancellationToken) =>
            {
                if (beforeCreate is not null)
                {
                    await beforeCreate(entity, cancellationToken);
                }

                var created = await store.CreateAsync(entity, cancellationToken);

                if (afterCreate is not null)
                {
                    await afterCreate(created, cancellationToken);
                }

                await store.SaveChangesAsync(cancellationToken);
                return Results.Created($"{routePattern}/{created.Id}", created);
            }).WithName($"{typeof(TEntity).Name}_Create");
        }

        if (crudOptions.EnableUpdate)
        {
            group.MapPut("/{id:int}", async (int id, TEntity entity, IHeadEntityStore<TEntity> store, HttpContext ctx, CancellationToken cancellationToken) =>
            {
                entity.Id = id;

                var existing = await store.GetAsync(id, cancellationToken);
                if (existing is null)
                {
                    return Results.NotFound();
                }

                if (ownershipExtractor is not null || authorizationPolicy is not null)
                {
                    var authorized = await checkAuthorization(existing, ctx, cancellationToken);
                    if (!authorized)
                    {
                        return Results.Forbid();
                    }
                }

                if (beforeUpdate is not null)
                {
                    await beforeUpdate(id, entity, cancellationToken);
                }

                var updated = await store.UpdateAsync(id, entity, cancellationToken);
                if (updated is null)
                {
                    return Results.NotFound();
                }

                if (afterUpdate is not null)
                {
                    await afterUpdate(id, updated, cancellationToken);
                }

                await store.SaveChangesAsync(cancellationToken);
                return Results.Ok(updated);
            }).WithName($"{typeof(TEntity).Name}_Update");
        }

        if (crudOptions.EnableDelete)
        {
            group.MapDelete("/{id:int}", async (int id, IHeadEntityStore<TEntity> store, HttpContext ctx, CancellationToken cancellationToken) =>
            {
                var existing = await store.GetAsync(id, cancellationToken);
                if (existing is null)
                {
                    return Results.NotFound();
                }

                if (ownershipExtractor is not null || authorizationPolicy is not null)
                {
                    var authorized = await checkAuthorization(existing, ctx, cancellationToken);
                    if (!authorized)
                    {
                        return Results.Forbid();
                    }
                }

                if (beforeDelete is not null)
                {
                    await beforeDelete(id, cancellationToken);
                }

                var deleted = await store.DeleteAsync(id, cancellationToken);
                if (deleted is null)
                {
                    return Results.NotFound();
                }

                if (afterDelete is not null)
                {
                    await afterDelete(deleted, cancellationToken);
                }

                await store.SaveChangesAsync(cancellationToken);
                return Results.Ok(deleted);
            }).WithName($"{typeof(TEntity).Name}_Delete");
        }

        foreach (var action in actions)
        {
            group.MapMethods("/{id:int}/" + action.Name, [action.HttpMethod], async (
                int id,
                IHeadEntityStore<TEntity> store,
                CancellationToken cancellationToken) =>
            {
                var entity = await store.GetAsync(id, cancellationToken);
                if (entity is null)
                {
                    return Results.NotFound();
                }

                await action.Handler(entity, cancellationToken);
                await store.SaveChangesAsync(cancellationToken);
                return Results.Ok(entity);
            }).WithName($"{typeof(TEntity).Name}_{action.Name}");
        }

        return group;
    }
}
