---
sidebar_position: 4
title: Authorization & Ownership
description: Restrict entity access to authorized users using ownership rules and custom authorization policies.
---

# Authorization & Ownership

Head.Net provides two ways to protect entity endpoints: **ownership extraction** for the common case where each entity belongs to one user, and **custom authorization policies** for more complex rules. Both integrate cleanly with the fluent builder without touching individual endpoint handlers.

## Ownership extraction

The most common authorization pattern for user-owned entities: only the owner can read, update, or delete their data.

```csharp
.RequireOwnership(invoice => invoice.UserId)
```

The lambda extracts the owner's user ID from the entity. On `GET`, `PUT`, `DELETE`, and custom actions, Head.Net:

1. Loads the entity
2. Extracts the owner ID using your lambda
3. Compares it to the current user's ID
4. Returns `403 Forbidden` if they do not match

### Entity setup

Add a `UserId` field to your entity:

```csharp
public sealed class Invoice : IHeadEntity<int>
{
    public int Id { get; set; }
    public int UserId { get; set; }  // owner
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Status { get; set; } = "draft";
    public DateTimeOffset CreatedAt { get; set; }
}
```

### Full example

```csharp
app.MapEntity<Invoice>()
    .WithCrud()
    .BeforeCreate((invoice, ct) =>
    {
        invoice.UserId = /* resolved from current user */;
        invoice.CreatedAt = DateTimeOffset.UtcNow;
        return ValueTask.CompletedTask;
    })
    .RequireOwnership(invoice => invoice.UserId)
    .Build();
```

## Custom authorization policy

For rules that are more nuanced than simple ownership — roles, time-based access, entity state conditions — use `RequireAuthorization`:

```csharp
.RequireAuthorization(async (invoice, userId, ct) =>
{
    // Admins can access any invoice
    if (await roleService.IsAdminAsync(userId, ct))
        return true;

    // Regular users can only access their own
    return invoice.UserId == userId;
})
```

The policy delegate receives:
- `TEntity entity` — the loaded entity
- `int userId` — the current user's ID (extracted by the user ID provider)
- `CancellationToken ct`

Return `true` to allow, `false` to return `403 Forbidden`.

## Configuring user ID extraction

Head.Net extracts the current user ID from the HTTP context. The default implementation reads the `"UserId"` claim from the authentication principal:

```csharp
// Default behavior (no configuration needed if you use this claim name)
ctx.User?.FindFirst("UserId")?.Value
```

If your auth scheme uses a different claim name or source, provide a custom extractor:

```csharp
.WithUserIdProvider(ctx =>
{
    var sub = ctx.User?.FindFirst("sub")?.Value;
    return int.TryParse(sub, out var id) ? id : 0;
})
```

Or extract from a header for API key scenarios:

```csharp
.WithUserIdProvider(ctx =>
{
    var header = ctx.Request.Headers["X-User-Id"].FirstOrDefault();
    return int.TryParse(header, out var id) ? id : 0;
})
```

A return value of `0` is treated as unauthenticated. Combined with `RequireOwnership`, any entity with a non-zero `UserId` will reject unauthenticated requests.

## Which endpoints are protected?

Ownership and authorization checks apply to:

| Operation | Protected? |
|---|---|
| `GET /entities` (list) | No — filtered via `WithQueryFilter` instead |
| `GET /entities/{id}` | Yes |
| `POST /entities` (create) | No — use `BeforeCreate` to set owner |
| `PUT /entities/{id}` | Yes |
| `DELETE /entities/{id}` | Yes |
| Custom actions | Yes |

The list endpoint is intentionally not protected by ownership at the individual entity level — use `WithQueryFilter` to scope the list to the current user instead:

```csharp
.WithQueryFilter(q => q.Where(inv => inv.UserId == currentUserId))
```

For this to work in a setup class, inject the user context through the constructor:

```csharp
public sealed class InvoiceSetup : IHeadEntitySetup<Invoice>
{
    private readonly ICurrentUser _currentUser;

    public InvoiceSetup(ICurrentUser currentUser) => _currentUser = currentUser;

    public void Configure(HeadEntityEndpointBuilder<Invoice> builder)
    {
        builder
            .WithQueryFilter(q => q.Where(inv => inv.UserId == _currentUser.Id))
            .RequireOwnership(inv => inv.UserId);
    }
}
```

## HeadAuthorizationResult

`HeadAuthorizationResult` is available if you need to express authorization outcomes explicitly in your own code:

```csharp
var result = HeadAuthorizationResult.Allow();
var denied = HeadAuthorizationResult.Deny("Invoice is locked and cannot be modified");

result.Allowed; // true
denied.Allowed; // false
denied.Reason;  // "Invoice is locked and cannot be modified"
```

## Combining ownership and authorization

`RequireOwnership` and `RequireAuthorization` are mutually exclusive — registering both will use the **authorization policy** and ignore the ownership extractor. Use `RequireAuthorization` when you need a superset of ownership logic:

```csharp
.RequireAuthorization(async (invoice, userId, ct) =>
{
    // Ownership check
    if (invoice.UserId == userId) return true;

    // Admin override
    return await admins.IsAdminAsync(userId, ct);
})
```

## Without ASP.NET Core authentication

Head.Net does not require ASP.NET Core's authentication middleware. If you extract user IDs another way (API key lookup, session cookie, database token), provide a `WithUserIdProvider` that returns the right ID for the current request.

For unauthenticated public APIs, simply omit `RequireOwnership` and `RequireAuthorization` entirely. No checks run by default.
