using System.Linq.Expressions;
using Head.Net.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Head.Net.AspNetCore;

/// <summary>
/// Builds a minimal API surface for an entity type using fluent configuration.
/// Generates CRUD endpoints (GET, POST, PUT, DELETE) and custom actions with support for:
/// - Authorization (ownership rules and custom policies)
/// - Lifecycle hooks (before/after Create, Update, Delete)
/// - Query filtering and pagination
/// - Custom domain actions (e.g., "pay", "void", "publish")
/// </summary>
/// <remarks>
/// ## Architecture
///
/// The builder uses a fluent API pattern for configuration. At Build() time, it orchestrates
/// endpoint generation using specialized handlers and shared services:
///
/// **Services (cross-cutting concerns):**
/// - <see cref="HeadUserContextService"/>: Extracts current user ID from HTTP context
/// - <see cref="HeadAuthorizationService{TEntity, TKey}"/>: Unified authorization for GET, UPDATE, DELETE
/// - <see cref="HeadHookExecutionService{TEntity, TKey}"/>: Manages before/after hooks for Create, Update, Delete
/// - <see cref="HeadQueryBuilderService{TEntity}"/>: Handles pagination and filtering for List endpoint
///
/// **Handlers (operation-specific logic):**
/// - <see cref="HeadListEndpointHandler{TEntity, TKey}"/>: Maps GET / with pagination
/// - <see cref="HeadGetEndpointHandler{TEntity, TKey}"/>: Maps GET /{id} with authorization
/// - <see cref="HeadCreateEndpointHandler{TEntity, TKey}"/>: Maps POST / with hooks
/// - <see cref="HeadUpdateEndpointHandler{TEntity, TKey}"/>: Maps PUT /{id} with authorization and hooks
/// - <see cref="HeadDeleteEndpointHandler{TEntity, TKey}"/>: Maps DELETE /{id} with authorization and hooks
/// - <see cref="HeadCustomActionEndpointHandler{TEntity, TKey}"/>: Maps POST /{id}/{actionName} with authorization
///
/// This separation enables:
/// - Independent testing of authorization, hooks, and query logic
/// - Clear extension points for Phase 3+ features
/// - Reduced code duplication across endpoints
///
/// ## Extension Points (Phase 3+)
///
/// **Validation Pipeline:**
/// Hook services can be extended to return validation results, enabling handlers to short-circuit
/// on validation errors instead of throwing exceptions.
///
/// **Soft Delete Support:**
/// Query builder can filter deleted entities. DELETE operation can mark as deleted instead of removing.
///
/// **Audit Logging:**
/// Hooks can track who changed what and when. Custom handlers can log entity changes.
///
/// **Role-Based Authorization:**
/// Authorization service can be extended with role checks, delegation rules, and detailed denial reasons.
///
/// **Advanced Filtering:**
/// Query builder can support dictionary-based filtering and ordering in Phase 3.
/// </remarks>
/// <typeparam name="TEntity">The entity type. Must implement <see cref="IHeadEntity{TKey}"/>.</typeparam>
/// <typeparam name="TKey">The entity's primary key type. Must be non-null and equatable.</typeparam>
public sealed class HeadEntityEndpointBuilder<TEntity, TKey>
    where TEntity : class, IHeadEntity<TKey>
    where TKey : notnull, IEquatable<TKey>
{
    private readonly IEndpointRouteBuilder endpoints;
    private readonly string routePattern;
    private readonly HeadCrudOptions crudOptions = new();
    private readonly List<HeadEntityActionDefinition<TEntity>> actions = [];
    private HeadBeforeCreateDelegate<TEntity>? beforeCreate;
    private HeadAfterCreateDelegate<TEntity>? afterCreate;
    private HeadBeforeUpdateDelegate<TEntity, TKey>? beforeUpdate;
    private HeadAfterUpdateDelegate<TEntity, TKey>? afterUpdate;
    private HeadBeforeDeleteDelegate<TEntity, TKey>? beforeDelete;
    private HeadAfterDeleteDelegate<TEntity>? afterDelete;
    private bool enablePaging = true;
    private int defaultPageSize = 100;
    private Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryFilter;
    private HeadOwnershipExtractor<TEntity>? ownershipExtractor;
    private HeadAuthorizationPolicyDelegate<TEntity>? authorizationPolicy;
    private Func<HttpContext, int>? userIdProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeadEntityEndpointBuilder{TEntity, TKey}"/> class.
    /// </summary>
    internal HeadEntityEndpointBuilder(IEndpointRouteBuilder endpoints, string routePattern)
    {
        this.endpoints = endpoints;
        this.routePattern = routePattern;
    }

    /// <summary>
    /// Example usage:
    /// <code>
    /// endpoints
    ///     .MapEntity&lt;Invoice&gt;("/invoices")
    ///     .WithCrud(opts => opts.EnableDelete = false)
    ///     .WithPaging(enable: true, defaultPageSize: 50)
    ///     .WithQueryFilter(q => q.Where(i => !i.IsArchived))
    ///     .RequireOwnership(i => i.UserId)
    ///     .BeforeCreate(async (inv, ct) => inv.CreatedAt = DateTime.UtcNow)
    ///     .AfterCreate(async (inv, ct) => await emailService.SendConfirmation(inv))
    ///     .CustomAction("pay", async (inv, ct) => await billing.Charge(inv), "POST")
    ///     .CustomAction("void", async (inv, ct) => await billing.Void(inv), "POST")
    ///     .Build();
    /// </code>
    /// </summary>

    /// <summary>
    /// Configures which CRUD operations are enabled.
    /// </summary>
    public HeadEntityEndpointBuilder<TEntity, TKey> WithCrud(Action<HeadCrudOptions>? configure = null)
    {
        configure?.Invoke(crudOptions);
        return this;
    }

    /// <summary>
    /// Registers a hook that runs before an entity is created.
    /// </summary>
    public HeadEntityEndpointBuilder<TEntity, TKey> BeforeCreate(HeadBeforeCreateDelegate<TEntity> hook)
    {
        beforeCreate = hook;
        return this;
    }

    /// <summary>
    /// Registers a hook that runs after an entity is created.
    /// </summary>
    public HeadEntityEndpointBuilder<TEntity, TKey> AfterCreate(HeadAfterCreateDelegate<TEntity> hook)
    {
        afterCreate = hook;
        return this;
    }

    /// <summary>
    /// Registers a hook that runs before an entity is updated.
    /// </summary>
    public HeadEntityEndpointBuilder<TEntity, TKey> BeforeUpdate(HeadBeforeUpdateDelegate<TEntity, TKey> hook)
    {
        beforeUpdate = hook;
        return this;
    }

    /// <summary>
    /// Registers a hook that runs after an entity is updated.
    /// </summary>
    public HeadEntityEndpointBuilder<TEntity, TKey> AfterUpdate(HeadAfterUpdateDelegate<TEntity, TKey> hook)
    {
        afterUpdate = hook;
        return this;
    }

    /// <summary>
    /// Registers a hook that runs before an entity is deleted.
    /// </summary>
    public HeadEntityEndpointBuilder<TEntity, TKey> BeforeDelete(HeadBeforeDeleteDelegate<TEntity, TKey> hook)
    {
        beforeDelete = hook;
        return this;
    }

    /// <summary>
    /// Registers a hook that runs after an entity is deleted.
    /// </summary>
    public HeadEntityEndpointBuilder<TEntity, TKey> AfterDelete(HeadAfterDeleteDelegate<TEntity> hook)
    {
        afterDelete = hook;
        return this;
    }

    /// <summary>
    /// Configures list endpoint paging behavior.
    /// </summary>
    public HeadEntityEndpointBuilder<TEntity, TKey> WithPaging(bool enable = true, int defaultPageSize = 100)
    {
        enablePaging = enable;
        this.defaultPageSize = defaultPageSize;
        return this;
    }

    /// <summary>
    /// Registers a named entity action that maps to <c>/{id}/{action}</c>.
    /// </summary>
    public HeadEntityEndpointBuilder<TEntity, TKey> CustomAction(
        string name,
        Func<TEntity, CancellationToken, Task> handler,
        string httpMethod = "POST")
    {
        actions.Add(new HeadEntityActionDefinition<TEntity>(name, httpMethod, handler));
        return this;
    }

    /// <summary>
    /// Registers a custom query filter to apply to list operations.
    /// Applied by <see cref="HeadQueryBuilderService{TEntity}"/> before pagination.
    /// </summary>
    /// <remarks>
    /// Example: `.WithQueryFilter(q => q.Where(e => !e.IsDeleted))`
    /// Phase 3+: Will support ordering and dictionary-based advanced filtering.
    /// </remarks>
    public HeadEntityEndpointBuilder<TEntity, TKey> WithQueryFilter(Func<IQueryable<TEntity>, IQueryable<TEntity>> filter)
    {
        queryFilter = filter;
        return this;
    }

    /// <summary>
    /// Registers an ownership check that restricts access to entities owned by the current user.
    /// Applied by <see cref="HeadAuthorizationService{TEntity, TKey}"/> on GET, UPDATE, DELETE operations.
    /// Checked after custom authorization policy (if configured) and before default allow.
    /// </summary>
    /// <remarks>
    /// Example: `.RequireOwnership(i => i.UserId)`
    /// The extractor returns the owner's user ID, which is compared to the current user.
    /// </remarks>
    public HeadEntityEndpointBuilder<TEntity, TKey> RequireOwnership(HeadOwnershipExtractor<TEntity> ownershipExtractor)
    {
        this.ownershipExtractor = ownershipExtractor;
        return this;
    }

    /// <summary>
    /// Registers a custom authorization policy that is checked first before ownership rules.
    /// Applied by <see cref="HeadAuthorizationService{TEntity, TKey}"/> on GET, UPDATE, DELETE operations.
    /// Supports complex authorization logic like role-based checks or delegation.
    /// </summary>
    /// <remarks>
    /// Example: `.RequireAuthorization(async (entity, userId, ct) => entity.UserId == userId || entity.CanDelegate)`
    /// Phase 3+: Will support role-based checks and detailed denial reasons.
    /// </remarks>
    public HeadEntityEndpointBuilder<TEntity, TKey> RequireAuthorization(HeadAuthorizationPolicyDelegate<TEntity> policy)
    {
        authorizationPolicy = policy;
        return this;
    }

    /// <summary>
    /// Configures how to extract the current user ID from the HTTP context.
    /// Default: parses the "UserId" claim from the authentication principal, returns 0 if not found.
    /// Used by <see cref="HeadAuthorizationService{TEntity, TKey}"/> and <see cref="HeadUserContextService"/>.
    /// </summary>
    /// <remarks>
    /// Example: `.WithUserIdProvider(ctx => int.Parse(ctx.User.FindFirst("sub").Value))`
    /// Default provider: `.WithUserIdProvider(ctx => ...)`
    /// </remarks>
    public HeadEntityEndpointBuilder<TEntity, TKey> WithUserIdProvider(Func<HttpContext, int> provider)
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
    /// <typeparam name="TSetup">A class implementing <see cref="IHeadEntitySetup{TEntity, TKey}"/>.</typeparam>
    public HeadEntityEndpointBuilder<TEntity, TKey> Setup<TSetup>()
        where TSetup : class, IHeadEntitySetup<TEntity, TKey>
    {
        var instance = ActivatorUtilities.CreateInstance<TSetup>(endpoints.ServiceProvider);
        instance.Configure(this);
        return this;
    }

    /// <summary>
    /// Maps the configured endpoints and returns the route group.
    /// </summary>
    /// <remarks>
    /// This method orchestrates endpoint generation using single-responsibility handlers
    /// and shared services. This separation enables:
    /// - Each CRUD operation to be tested in isolation
    /// - Authorization, hooks, and query logic to be extended independently
    /// - Clear extension points for Phase 3+ features (validation, soft delete, audit logging)
    ///
    /// Shared services handle cross-cutting concerns:
    /// - <see cref="HeadAuthorizationService{TEntity, TKey}"/>: Ownership and policy checks for GET, UPDATE, DELETE
    /// - <see cref="HeadHookExecutionService{TEntity, TKey}"/>: Lifecycle hooks for CREATE, UPDATE, DELETE
    /// - <see cref="HeadQueryBuilderService{TEntity}"/>: Pagination and filtering for LIST
    /// - <see cref="HeadUserContextService"/>: User ID extraction from HTTP context
    ///
    /// Each handler (e.g., <see cref="HeadListEndpointHandler{TEntity, TKey}"/>,
    /// <see cref="HeadGetEndpointHandler{TEntity, TKey}"/>, etc.) maps one CRUD operation
    /// and uses these services to implement consistent behavior across endpoints.
    /// </remarks>
    public RouteGroupBuilder Build()
    {
        var group = endpoints.MapGroup(routePattern)
            .WithTags(typeof(TEntity).Name);

        // Initialize shared services for cross-cutting concerns.
        // These services encapsulate logic that would otherwise be duplicated across endpoint handlers.
        var userService = new HeadUserContextService(userIdProvider);
        var authService = new HeadAuthorizationService<TEntity, TKey>(authorizationPolicy, ownershipExtractor, userIdProvider);
        var hookService = new HeadHookExecutionService<TEntity, TKey>(beforeCreate, afterCreate, beforeUpdate, afterUpdate, beforeDelete, afterDelete);
        var queryService = new HeadQueryBuilderService<TEntity>(queryFilter, enablePaging, defaultPageSize);

        // Map CRUD endpoints based on configuration.
        // Each handler is responsible for one HTTP operation and uses the shared services above.
        if (crudOptions.EnableList)
        {
            HeadListEndpointHandler<TEntity, TKey>.Map(group, queryService);
        }

        if (crudOptions.EnableGet)
        {
            HeadGetEndpointHandler<TEntity, TKey>.Map(group, authService, userService);
        }

        if (crudOptions.EnableCreate)
        {
            HeadCreateEndpointHandler<TEntity, TKey>.Map(group, hookService, routePattern);
        }

        if (crudOptions.EnableUpdate)
        {
            HeadUpdateEndpointHandler<TEntity, TKey>.Map(group, authService, hookService, userService);
        }

        if (crudOptions.EnableDelete)
        {
            HeadDeleteEndpointHandler<TEntity, TKey>.Map(group, authService, hookService, userService);
        }

        // Map custom action endpoints.
        // Custom actions now also use authorization service for consistent access control.
        foreach (var action in actions)
        {
            HeadCustomActionEndpointHandler<TEntity, TKey>.Map(group, authService, action);
        }

        return group;
    }
}
