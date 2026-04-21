---
sidebar_position: 1
title: Entity Lifecycle Hooks
description: Run domain logic before and after every CRUD operation without leaking HTTP concerns into your business code.
---

# Entity Lifecycle Hooks

Hooks are the primary extension point in Head.Net. They run at defined points in the CRUD pipeline and give you a clean place to put domain logic — timestamps, notifications, audit trails, derived state — without touching HTTP request or response objects.

## Available hooks

| Hook | When it runs | Return type | Can abort? |
|------|-------------|-------------|-----------|
| `BeforeCreate` | Before the entity is persisted | `ValueTask<HeadHookResult<TEntity>?>` | ✅ Yes — return `Invalid(...)` |
| `AfterCreate` | After the entity is persisted | `ValueTask` | No |
| `BeforeUpdate` | Before the entity is updated | `ValueTask<HeadHookResult<TEntity>?>` | ✅ Yes — return `Invalid(...)` |
| `AfterUpdate` | After the entity is updated | `ValueTask` | No |
| `BeforeDelete` | Before the entity is removed | `ValueTask` | No |
| `AfterDelete` | After the entity is removed | `ValueTask` | No |

`BeforeCreate` and `BeforeUpdate` return `HeadHookResult<TEntity>?`. Return `null` to proceed normally, or `HeadHookResult<TEntity>.Invalid(validation)` to abort with a `400 Validation Failed` response. All other hooks are fire-and-forget `ValueTask`.

---

## BeforeCreate

Use `BeforeCreate` to set derived fields, defaults, or computed state before the entity reaches the database. Return `null` to proceed, or a validation result to abort with `400`.

```csharp
.BeforeCreate((invoice, ct) =>
{
    invoice.CreatedAt = DateTimeOffset.UtcNow;
    invoice.Status = "draft";
    invoice.ReferenceNumber = GenerateReference();
    return new ValueTask<HeadHookResult<Invoice>?>((HeadHookResult<Invoice>?)null); // null = proceed
})
```

The entity object passed to `BeforeCreate` is the same instance that will be persisted. Mutations here are reflected in the database and in the `201 Created` response.

## AfterCreate

`AfterCreate` runs after the entity has been saved. The entity has an assigned `Id` at this point.

```csharp
.AfterCreate(async (invoice, ct) =>
{
    await emailService.SendConfirmationAsync(invoice.CustomerEmail, invoice.Id, ct);
    await auditLog.RecordAsync("Invoice created", invoice.Id, ct);
})
```

:::info
If `AfterCreate` mutates the entity (for example, setting a computed field), `SaveChangesAsync` is called automatically after the hook runs. Mutations are safe here.
:::

## BeforeUpdate

`BeforeUpdate` receives the entity ID and the incoming replacement data. Use it to validate the incoming data, reject illegal state transitions, or stamp an audit timestamp. Return `null` to proceed, or a validation result to abort with `400`.

```csharp
.BeforeUpdate((id, invoice, ct) =>
{
    invoice.UpdatedAt = DateTimeOffset.UtcNow;
    return new ValueTask<HeadHookResult<Invoice>?>((HeadHookResult<Invoice>?)null); // null = proceed
})
```

Note that `BeforeUpdate` receives the **incoming** entity, not the existing one. If you need the existing record, fetch it from the store or inject a service.

## AfterUpdate

`AfterUpdate` runs after the update has been persisted. It receives the ID and the updated entity.

```csharp
.AfterUpdate(async (id, invoice, ct) =>
{
    if (invoice.Status == "paid")
    {
        await billing.RecordPaymentAsync(invoice.Id, invoice.Total, ct);
    }
})
```

## BeforeDelete

`BeforeDelete` receives only the entity ID. Use it for pre-deletion validation, archiving related data, or audit logging.

```csharp
.BeforeDelete(async (id, ct) =>
{
    var hasRelatedOrders = await orders.ExistsForInvoiceAsync(id, ct);
    if (hasRelatedOrders)
    {
        // See Validation section below for how to block this
    }
    await auditLog.RecordAsync("Invoice delete requested", id, ct);
})
```

## AfterDelete

`AfterDelete` receives the deleted entity. The entity is no longer in the database at this point.

```csharp
.AfterDelete(async (invoice, ct) =>
{
    await searchIndex.RemoveAsync(invoice.Id, ct);
    await cache.InvalidateAsync($"invoice:{invoice.Id}", ct);
})
```

---

## Async hooks

All hooks accept async lambdas. Use `async`/`await` freely:

```csharp
.AfterCreate(async (invoice, ct) =>
{
    await Task.WhenAll(
        notifications.PushAsync(invoice.Id, ct),
        analytics.TrackCreateAsync(invoice, ct)
    );
})
```

---

## Multiple hooks on the same operation

Each hook slot holds a single delegate. If you need multiple operations in one hook, compose them inline or delegate to a service:

```csharp
.AfterCreate(async (invoice, ct) =>
{
    await invoiceService.HandleCreatedAsync(invoice, ct);
    // invoiceService.HandleCreatedAsync internally does notifications + audit
})
```

Or use a setup class (see [Setup Classes](./setup-classes)) and inject an `InvoiceEventService` through the constructor.

---

## Validation and short-circuiting

`BeforeCreate` and `BeforeUpdate` support clean short-circuit validation. Return `HeadHookResult<TEntity>.Invalid(validation)` to abort the operation and respond with `400 Validation Failed` (RFC 7807 Problem Details). No exception throwing required.

```csharp
.BeforeCreate((invoice, ct) =>
{
    if (invoice.Total <= 0)
    {
        var validation = HeadValidationResult.Failure("Total must be greater than zero");
        return new ValueTask<HeadHookResult<Invoice>?>(HeadHookResult<Invoice>.Invalid(validation));
    }
    invoice.CreatedAt = DateTimeOffset.UtcNow;
    return new ValueTask<HeadHookResult<Invoice>?>((HeadHookResult<Invoice>?)null); // proceed
})
```

Multiple errors can be returned at once:

```csharp
var errors = new List<string>();
if (string.IsNullOrWhiteSpace(invoice.CustomerName)) errors.Add("CustomerName is required");
if (invoice.Total <= 0) errors.Add("Total must be positive");

if (errors.Count > 0)
{
    var validation = HeadValidationResult.Failure(errors.ToArray());
    return new ValueTask<HeadHookResult<Invoice>?>(HeadHookResult<Invoice>.Invalid(validation));
}
```

The resulting error response:

```json
{
  "type": "https://head.net/errors/validation-failed",
  "title": "Validation Failed",
  "detail": "CustomerName is required; Total must be positive.",
  "status": 400
}
```

---

## Organizing hooks

For entities with many hooks, inline lambdas in `Program.cs` get noisy quickly. The recommended pattern is a [Setup Class](./setup-classes):

```csharp
// Program.cs — clean
app.MapEntity<Invoice>()
    .WithCrud()
    .Setup<InvoiceSetup>()
    .Build();

// InvoiceSetup.cs — all hooks in one place
public sealed class InvoiceSetup : IHeadEntitySetup<Invoice>
{
    private readonly IInvoiceService _invoiceService;

    public InvoiceSetup(IInvoiceService invoiceService)
        => _invoiceService = invoiceService;

    public void Configure(HeadEntityEndpointBuilder<Invoice> builder)
    {
        builder
            .BeforeCreate(OnBeforeCreate)
            .AfterCreate(OnAfterCreate)
            .BeforeDelete(OnBeforeDelete);
    }

    private ValueTask OnBeforeCreate(Invoice invoice, CancellationToken ct)
    {
        invoice.CreatedAt = DateTimeOffset.UtcNow;
        invoice.Status = "draft";
        return ValueTask.CompletedTask;
    }

    private async ValueTask OnAfterCreate(Invoice invoice, CancellationToken ct)
        => await _invoiceService.NotifyCreatedAsync(invoice, ct);

    private async ValueTask OnBeforeDelete(int id, CancellationToken ct)
        => await _invoiceService.ValidateDeletableAsync(id, ct);
}
```

The setup class receives `IInvoiceService` from DI automatically — no registration of `InvoiceSetup` itself required.
