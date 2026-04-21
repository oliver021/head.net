---
sidebar_position: 2
title: Custom Actions
description: Add named domain actions to your entity surface — beyond standard CRUD.
---

# Custom Actions

CRUD covers the structural operations on an entity: create it, read it, update it, delete it. But real systems have domain operations that do not map cleanly to these five verbs. An invoice gets *paid*. A document gets *archived*. An order gets *fulfilled*.

Custom actions let you declare these operations as first-class members of the entity surface.

## Declaring a custom action

```csharp
app.MapEntity<Invoice>()
    .WithCrud()
    .CustomAction("pay", (invoice, ct) =>
    {
        invoice.Status = "paid";
        invoice.PaidAt = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    })
    .Build();
```

This registers `POST /invoices/{id}/pay`.

## What happens at runtime

1. The request arrives at `POST /invoices/{id}/pay`
2. Head.Net fetches the entity with the given `{id}` — returns `404` if not found
3. Your handler runs with the loaded entity and the cancellation token
4. `SaveChangesAsync` is called automatically to persist any mutations
5. The updated entity is returned with `200 OK`

You never write the fetch, the save, or the 404 check.

## Signature

```csharp
.CustomAction(
    string name,
    Func<TEntity, CancellationToken, Task> handler,
    string httpMethod = "POST"
)
```

- **`name`** — used as the route segment and the OpenAPI operation name (`Invoice_pay`)
- **`handler`** — receives the loaded entity; mutations are persisted automatically
- **`httpMethod`** — defaults to `POST`; pass `"PUT"` or `"PATCH"` if semantics require it

## Multiple actions on one entity

```csharp
.CustomAction("pay", (invoice, ct) =>
{
    invoice.Status = "paid";
    invoice.PaidAt = DateTimeOffset.UtcNow;
    return Task.CompletedTask;
})
.CustomAction("archive", (invoice, ct) =>
{
    invoice.Status = "archived";
    return Task.CompletedTask;
})
.CustomAction("void", (invoice, ct) =>
{
    invoice.Status = "void";
    invoice.VoidedAt = DateTimeOffset.UtcNow;
    return Task.CompletedTask;
})
```

Each action becomes a separate route:
- `POST /invoices/{id}/pay`
- `POST /invoices/{id}/archive`
- `POST /invoices/{id}/void`

## Async actions with external services

Handlers are `Func<TEntity, CancellationToken, Task>`, so you can `await` freely. The standard pattern for service dependencies is to capture them in a setup class constructor:

```csharp
public sealed class InvoiceSetup : IHeadEntitySetup<Invoice>
{
    private readonly IBillingService _billing;
    private readonly INotificationService _notifications;

    public InvoiceSetup(IBillingService billing, INotificationService notifications)
    {
        _billing = billing;
        _notifications = notifications;
    }

    public void Configure(HeadEntityEndpointBuilder<Invoice> builder)
    {
        builder
            .CustomAction("pay", async (invoice, ct) =>
            {
                await _billing.ChargeAsync(invoice.Id, invoice.Total, ct);
                invoice.Status = "paid";
                invoice.PaidAt = DateTimeOffset.UtcNow;
                await _notifications.SendReceiptAsync(invoice.CustomerEmail, ct);
            })
            .CustomAction("refund", async (invoice, ct) =>
            {
                await _billing.RefundAsync(invoice.Id, ct);
                invoice.Status = "refunded";
                invoice.RefundedAt = DateTimeOffset.UtcNow;
            });
    }
}
```

`IBillingService` and `INotificationService` are injected from DI into `InvoiceSetup` at startup. The action lambdas capture them by closure.

## OpenAPI

Custom actions appear in OpenAPI automatically. For an `Invoice` entity with a `pay` action:

- **Operation ID**: `Invoice_pay`
- **Path**: `/invoices/{id}/pay`
- **Method**: `POST` (or whatever method you specified)
- **Responses**: `200` with the updated entity, `404` if not found

No additional attributes or annotations needed.

## HTTP method conventions

| Action type | Recommended method |
|---|---|
| State transition (pay, approve, publish) | `POST` |
| Reversible toggle (archive/unarchive) | `POST` (with separate actions) |
| Partial update with semantic meaning | `PATCH` |
| Replace-and-confirm semantics | `PUT` |

```csharp
.CustomAction("publish", PublishHandler, "POST")
.CustomAction("unpublish", UnpublishHandler, "POST")
.CustomAction("feature", FeatureHandler, "PATCH")
```

## Differences from BeforeUpdate / AfterUpdate

| | Custom Action | Update hooks |
|---|---|---|
| Triggered by | `POST /entity/{id}/name` | `PUT /entity/{id}` |
| Receives request body | No (entity only) | Yes (incoming entity) |
| HTTP semantics | Domain operation | Replace |
| Use case | `pay`, `archive`, `approve` | General field updates |

Custom actions are for named domain transitions. Update hooks are for processing general field updates. They serve different purposes and compose well together.
