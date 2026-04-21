---
sidebar_position: 4
title: Development Philosophy
description: The three core ideas that drive every design decision in Head.Net.
---

# Development Philosophy

Understanding *why* Head.Net is designed the way it is makes it easier to use correctly, extend confidently, and know when to step outside it.

There are three core ideas. Everything in the API follows from them.

---

## 1. Entities are the center

The most natural organizing unit for a data-backed API is the entity. Not the controller. Not the repository. The entity.

When you look at real systems, the behavioral richness lives on entities: invoices get paid, documents get archived, orders get fulfilled. The HTTP layer is just the delivery mechanism.

Head.Net starts from this premise and builds outward. You define the entity and its operations. The SDK generates the transport.

This is why `MapEntity<Invoice>()` is the entry point, not something like `MapControllers()` or `UseRepository<Invoice>()`. The entity is in charge.

---

## 2. CRUD is boilerplate, hooks are the work

CRUD is infrastructure. It is the same five operations repeated across every entity in every project. List, Get, Create, Update, Delete. Nobody learns anything new writing it for the fourth time.

Head.Net treats CRUD as something to be generated, not authored.

But hooks are different. A `BeforeCreate` hook that validates ownership, stamps a timestamp, and enqueues a notification — that is actual domain logic. That is where the team's knowledge about the business lives.

This is why hooks are first-class in Head.Net while CRUD is configuration. The SDK handles the part that is always the same. You own the part that is specific to your domain.

```csharp
// Head.Net handles this
.WithCrud()

// You own this
.BeforeCreate((invoice, ct) =>
{
    invoice.CreatedAt = DateTimeOffset.UtcNow;
    return ValueTask.CompletedTask;
})
.AfterCreate(async (invoice, ct) =>
{
    await notifications.SendAsync($"Invoice {invoice.Id} created", ct);
})
```

---

## 3. Domain actions are not HTTP patterns

Teams that use standard CRUD endpoints for everything end up contorting domain operations into PUT requests or inventing ad-hoc URL conventions. Both approaches make the API harder to understand and harder to evolve.

Head.Net gives you a first-class concept for domain actions: named operations that live on the entity surface, route predictably, and appear in OpenAPI without extra work.

```csharp
.CustomAction("pay", (invoice, ct) =>
{
    invoice.Status = "paid";
    invoice.PaidAt = DateTimeOffset.UtcNow;
    return Task.CompletedTask;
})
```

This generates `POST /invoices/{id}/pay`. The action name appears in OpenAPI as `Invoice_pay`. It is not a workaround — it is a deliberate design.

---

## What this means in practice

### Prefer setup classes over inline chains

Long fluent chains in `Program.cs` work but grow quickly. Head.Net supports `IHeadEntitySetup<TEntity>` to move configuration into a dedicated class — the same pattern EF Core uses for `IEntityTypeConfiguration<T>`.

```csharp
// In Program.cs — stays clean
app.MapEntity<Invoice>().WithCrud().Setup<InvoiceSetup>().Build();

// In InvoiceSetup.cs — all the domain logic in one place
public sealed class InvoiceSetup : IHeadEntitySetup<Invoice>
{
    public void Configure(HeadEntityEndpointBuilder<Invoice> builder) { ... }
}
```

### Keep hooks free of HTTP concerns

A `BeforeCreate` hook should not return 400 Bad Request. It should validate, set state, or short-circuit using `HeadValidationResult`. The SDK translates that into the appropriate HTTP response. Mixing HTTP concerns into hooks breaks the boundary between transport and domain.

### Use escape hatches confidently

Head.Net generates standard Minimal API handlers. If an entity needs a custom endpoint that does not fit the CRUD model, register it directly alongside Head.Net:

```csharp
app.MapEntity<Invoice>().WithCrud().Setup<InvoiceSetup>().Build();
app.MapGet("/invoices/summary", async (AppDbContext db) => ...); // Standard Minimal API
```

Head.Net does not own the router. It participates in it.

---

## The trust principle

Senior .NET developers are skeptical of "magic" libraries — for good reason. Generic abstractions that hide what is happening make production incidents harder to diagnose and integrations harder to trust.

Head.Net is designed to be transparent:

- Generated behavior maps to readable Minimal API handler code
- Conventions are documented, not inferred
- Every hook and action is a plain delegate you can read, debug, and test
- No reflection tricks at request time — handlers are wired at startup

Phase 4 of the roadmap introduces diagnostics that explain exactly what was generated and why, and evaluates source generation to remove runtime reflection entirely.

---

These three ideas — entities at center, CRUD as infrastructure, domain actions as first-class — are the lens through which every feature decision is made. When something in Head.Net seems surprising, tracing it back to these principles usually explains the choice.
