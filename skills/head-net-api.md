---
name: head-net-api
description: >
  Guide for AI agents working with the Head.Net SDK. Teaches how to wire up CRUD
  endpoints, lifecycle hooks, validation, custom domain actions, and authorization
  for any entity — with concrete code patterns for each step.
type: skill
version: "1.0"
invocation: /head-net
allowed-tools:
  - Read
  - Glob
  - Grep
  - Edit
  - Write
  - Bash
---

# Head.Net API Agent Skill

This skill activates when the user asks you to:
- **Add CRUD** for a new entity
- **Add hooks** (BeforeCreate, AfterCreate, BeforeUpdate, etc.)
- **Validate** entity data before create or update
- **Add a custom domain action** (e.g. "pay", "archive", "approve")
- **Protect endpoints** with ownership or custom authorization
- **Register a new entity** in the application

When invoked, follow the decision tree below — read the current codebase state first, then apply only what the request requires.

---

## Step 0 — Understand the codebase before writing anything

Before generating code, run these reads:

```
1. Glob: src/**/*.cs           → find existing entities and DbContext
2. Grep: IHeadEntity           → confirm entity base types
3. Read: samples/Head.Net.SampleApi/Program.cs   → see how stores and endpoints are wired
4. Read: samples/Head.Net.SampleApi/InvoiceSetup.cs → see a complete setup class example
```

Key files to understand:
| File | Purpose |
|------|---------|
| `src/Head.Net.Abstractions/IHeadEntity.cs` | Entity base interface — every entity needs `IHeadEntity<TKey>` |
| `src/Head.Net.Abstractions/IHeadEntityStore.cs` | CRUD store contract |
| `src/Head.Net.Abstractions/HeadEntityHooks.cs` | Hook delegate signatures |
| `src/Head.Net.AspNetCore/HeadEntityEndpointBuilder.cs` | Fluent builder reference |
| `src/Head.Net.AspNetCore/IHeadEntitySetup.cs` | Setup class interface |

---

## Step 1 — Defining an entity

Every entity must implement `IHeadEntity<TKey>`. The key type (`int`, `Guid`, `long`, `string`) must satisfy `notnull, IEquatable<TKey>`.

```csharp
// Minimal entity (int key — most common)
public sealed class Product : IHeadEntity<int>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// Entity with Guid primary key
public sealed class Order : IHeadEntity<Guid>
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
}
```

Add the `DbSet<T>` to the existing `DbContext`:

```csharp
public DbSet<Product> Products => Set<Product>();
```

---

## Step 2 — Registering the store

Call `AddHeadEntityStore` once per entity in the service registration block.

```csharp
// Convenience overload — TKey defaults to int
builder.Services.AddHeadEntityStore<AppDbContext, Product>();

// Explicit TKey — use when key is not int
builder.Services.AddHeadEntityStore<AppDbContext, Order, Guid>();
```

---

## Step 3 — Mapping endpoints

After `builder.Build()`, map the entity with the fluent builder:

```csharp
// Minimal — all five CRUD operations enabled
app.MapEntity<Product>()
    .WithCrud()
    .Build();

// With specific operations disabled
app.MapEntity<Product>()
    .WithCrud(opts => opts.EnableDelete = false)
    .Build();

// With explicit TKey (required for non-int keys)
app.MapEntity<Order, Guid>()
    .WithCrud()
    .Build();
```

This generates these routes automatically:

| Method | Route | Operation |
|--------|-------|-----------|
| `GET` | `/products` | List (paged) |
| `GET` | `/products/{id}` | Get single |
| `POST` | `/products` | Create |
| `PUT` | `/products/{id}` | Update |
| `DELETE` | `/products/{id}` | Delete |

Route name is derived from the entity name (`Product` → `/products`). Pass an explicit pattern to override: `MapEntity<Product>("/api/v1/products")`.

---

## Step 4 — Adding lifecycle hooks

### Hook return types (important)

| Hook | Return type | Behavior |
|------|-------------|---------|
| `BeforeCreate` | `ValueTask<HeadHookResult<TEntity>?>` | `null` = proceed; `Invalid(...)` = abort with 400 |
| `AfterCreate` | `ValueTask` | Fire-and-forget after save |
| `BeforeUpdate` | `ValueTask<HeadHookResult<TEntity>?>` | `null` = proceed; `Invalid(...)` = abort with 400 |
| `AfterUpdate` | `ValueTask` | Fire-and-forget after save |
| `BeforeDelete` | `ValueTask` | Fire-and-forget before delete |
| `AfterDelete` | `ValueTask` | Fire-and-forget after delete |

### BeforeCreate — set defaults, stamp timestamps

```csharp
.BeforeCreate((product, ct) =>
{
    product.CreatedAt = DateTimeOffset.UtcNow;
    product.Status = "active";
    return new ValueTask<HeadHookResult<Product>?>((HeadHookResult<Product>?)null); // null = success
})
```

### BeforeCreate with validation — short-circuit and return 400

```csharp
.BeforeCreate((product, ct) =>
{
    if (string.IsNullOrWhiteSpace(product.Name))
    {
        var validation = HeadValidationResult.Failure("Name is required");
        return new ValueTask<HeadHookResult<Product>?>(HeadHookResult<Product>.Invalid(validation));
    }
    product.CreatedAt = DateTimeOffset.UtcNow;
    return new ValueTask<HeadHookResult<Product>?>((HeadHookResult<Product>?)null);
})
```

### BeforeUpdate — validate incoming state

```csharp
.BeforeUpdate((id, product, ct) =>
{
    if (product.Price < 0)
    {
        var validation = HeadValidationResult.Failure("Price cannot be negative");
        return new ValueTask<HeadHookResult<Product>?>(HeadHookResult<Product>.Invalid(validation));
    }
    return new ValueTask<HeadHookResult<Product>?>((HeadHookResult<Product>?)null);
})
```

### AfterCreate — side effects after save

```csharp
.AfterCreate(async (product, ct) =>
{
    await searchIndex.IndexAsync(product, ct);
})
```

### BeforeDelete — audit or guard

```csharp
.BeforeDelete(async (id, ct) =>
{
    await auditLog.RecordDeletionAsync("Product", id, ct);
})
```

---

## Step 5 — Adding custom domain actions

Custom actions map to `POST /{entity}/{id}/{actionName}`. They receive the loaded entity and return it after mutation.

```csharp
.CustomAction("activate", (product, ct) =>
{
    product.Status = "active";
    product.ActivatedAt = DateTimeOffset.UtcNow;
    return Task.CompletedTask;
})

.CustomAction("deactivate", (product, ct) =>
{
    product.Status = "inactive";
    return Task.CompletedTask;
})
```

Generated routes:

```
POST /products/{id}/activate
POST /products/{id}/deactivate
```

Authorization checks run on custom actions too — if `RequireOwnership` or `RequireAuthorization` is configured, the entity owner check happens before the action handler is invoked.

---

## Step 6 — Protecting endpoints

### Ownership — entity belongs to one user

```csharp
.RequireOwnership(product => product.OwnerId)
```

The lambda returns the owner's user ID. Head.Net compares it to the current user and returns `403` if they don't match.

Set the owner in `BeforeCreate`:

```csharp
.BeforeCreate((product, ct) =>
{
    product.OwnerId = /* extract from HTTP context via injected service */;
    return new ValueTask<HeadHookResult<Product>?>((HeadHookResult<Product>?)null);
})
.RequireOwnership(product => product.OwnerId)
```

### Custom authorization policy — complex rules

```csharp
.RequireAuthorization(async (product, userId, ct) =>
{
    if (await roleService.IsAdminAsync(userId, ct)) return true;
    return product.OwnerId == userId;
})
```

### Scoping the list endpoint to the current user

The list endpoint is not covered by ownership checks — use `WithQueryFilter` instead:

```csharp
.WithQueryFilter(q => q.Where(p => p.OwnerId == currentUserId))
```

Inject `ICurrentUser` or similar through a setup class constructor to access the user in a query filter.

### Configuring user ID extraction

```csharp
.WithUserIdProvider(ctx =>
{
    var sub = ctx.User?.FindFirst("sub")?.Value;
    return int.TryParse(sub, out var id) ? id : 0;
})
```

---

## Step 7 — Organizing with a setup class

When an entity has multiple hooks, actions, and authorization rules, a setup class keeps `Program.cs` clean. Setup classes receive constructor dependencies from DI automatically — no registration required.

```csharp
// Program.cs — stays minimal
app.MapEntity<Product>()
    .WithCrud()
    .Setup<ProductSetup>()
    .Build();

// ProductSetup.cs — all configuration in one class
public sealed class ProductSetup : IHeadEntitySetup<Product, int>
{
    private readonly ISearchIndex _search;
    private readonly IAuditLog _audit;

    public ProductSetup(ISearchIndex search, IAuditLog audit)
    {
        _search = search;
        _audit = audit;
    }

    public void Configure(HeadEntityEndpointBuilder<Product, int> builder)
    {
        builder
            .WithPaging(enable: true, defaultPageSize: 50)
            .BeforeCreate(OnBeforeCreate)
            .AfterCreate(OnAfterCreate)
            .BeforeDelete(OnBeforeDelete)
            .CustomAction("activate", OnActivate)
            .RequireOwnership(p => p.OwnerId);
    }

    private ValueTask<HeadHookResult<Product>?> OnBeforeCreate(Product product, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
        {
            var v = HeadValidationResult.Failure("Name is required");
            return new ValueTask<HeadHookResult<Product>?>(HeadHookResult<Product>.Invalid(v));
        }
        product.CreatedAt = DateTimeOffset.UtcNow;
        return new ValueTask<HeadHookResult<Product>?>((HeadHookResult<Product>?)null);
    }

    private async ValueTask OnAfterCreate(Product product, CancellationToken ct)
        => await _search.IndexAsync(product, ct);

    private async ValueTask OnBeforeDelete(int id, CancellationToken ct)
        => await _audit.RecordAsync("Product deleted", id, ct);

    private Task OnActivate(Product product, CancellationToken ct)
    {
        product.Status = "active";
        return Task.CompletedTask;
    }
}
```

---

## Step 8 — Paging and filtering

```csharp
// Enable paging with custom default page size
.WithPaging(enable: true, defaultPageSize: 25)

// Apply a global query filter
.WithQueryFilter(q => q.Where(p => !p.IsArchived))
```

Clients control paging via query string:

```
GET /products?skip=0&take=25
```

Response shape:

```json
{
  "data": [...],
  "totalCount": 150,
  "skip": 0,
  "take": 25,
  "pageCount": 6,
  "pageNumber": 0
}
```

---

## Error response format (RFC 7807)

All error responses follow Problem Details format:

```json
// 404 Not Found
{
  "type": "https://head.net/errors/not-found",
  "title": "Entity Not Found",
  "detail": "Product with ID 42 not found.",
  "status": 404
}

// 400 Validation Failed
{
  "type": "https://head.net/errors/validation-failed",
  "title": "Validation Failed",
  "detail": "Name is required; Price cannot be negative.",
  "status": 400
}

// 403 Forbidden
// Returns HTTP 403 when ownership or authorization check fails.
```

---

## Common user requests → which steps apply

| User asks for... | Steps |
|-----------------|-------|
| "Add CRUD for Product" | 1 → 2 → 3 |
| "Add a BeforeCreate hook" | 4 (BeforeCreate only) |
| "Validate data before saving" | 4 (BeforeCreate/BeforeUpdate with `HeadHookResult`) |
| "Add a pay / approve / archive action" | 5 |
| "Restrict access to owners" | 6 (RequireOwnership) |
| "Add role-based access" | 6 (RequireAuthorization) |
| "Organize into a setup class" | 7 |
| "Add paging" | 8 |
| "Filter the list to current user" | 8 (WithQueryFilter) |
| "Add audit logging on delete" | 4 (BeforeDelete or AfterDelete) |

---

## Checklist before finishing

After generating or modifying code, verify:

- [ ] Entity implements `IHeadEntity<TKey>` with the right key type
- [ ] `DbSet<T>` added to the DbContext
- [ ] `AddHeadEntityStore` registered in service collection
- [ ] `MapEntity<T>().WithCrud().Build()` called in endpoint mapping
- [ ] Setup class implements `IHeadEntitySetup<TEntity, TKey>` (not the old single-arg form)
- [ ] `BeforeCreate`/`BeforeUpdate` return `ValueTask<HeadHookResult<TEntity>?>` (null = success)
- [ ] `AfterCreate`, `AfterUpdate`, `BeforeDelete`, `AfterDelete` return `ValueTask`
- [ ] Build passes: `dotnet build Head.Net.sln` — 0 warnings, 0 errors
- [ ] Tests pass: `dotnet test Head.Net.sln`
