---
slug: /
sidebar_position: 1
title: Introduction
description: Head.Net — the EF Core SDK that collapses the CRUD tax on .NET APIs.
---

# You've written this before

You're about to build a new internal API. There's an `Invoice` entity, some business rules around it, and it needs five standard HTTP endpoints.

Before you write a single line of domain logic, you'll spend the next few hours on:

- `IInvoiceRepository.cs` — the interface
- `InvoiceRepository.cs` — the EF Core implementation
- `IInvoiceService.cs` — the service interface
- `InvoiceService.cs` — the service calling the repository
- `InvoiceController.cs` (or five separate endpoint files) — the HTTP surface
- OpenAPI annotations, response type declarations, route naming

None of that is the interesting part. The interesting part is the `BeforeCreate` hook that sets the due date, the `AfterCreate` hook that sends a confirmation email, and the `pay` action that charges the customer. That's the work that matters. Everything else is infrastructure you've already written a dozen times.

**Head.Net exists to make that overhead disappear.**

---

## The before and after

Here is a complete, production-ready API for the same `Invoice` entity — full CRUD, lifecycle hooks, a custom domain action, paging, and EF Core persistence:

```csharp title="Program.cs"
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connString));
builder.Services.AddHeadEntityStore<AppDbContext, Invoice>();

var app = builder.Build();

app.MapEntity<Invoice>()
    .WithCrud()
    .Setup<InvoiceSetup>()
    .Build();
```

```csharp title="InvoiceSetup.cs"
public sealed class InvoiceSetup : IHeadEntitySetup<Invoice>
{
    private readonly IEmailService _email;

    public InvoiceSetup(IEmailService email) => _email = email;

    public void Configure(HeadEntityEndpointBuilder<Invoice> builder)
    {
        builder
            .WithPaging(defaultPageSize: 50)
            .BeforeCreate((invoice, _) =>
            {
                invoice.CreatedAt = DateTimeOffset.UtcNow;
                invoice.Status = "draft";
                return ValueTask.CompletedTask;
            })
            .AfterCreate(async (invoice, ct) =>
                await _email.SendConfirmationAsync(invoice.CustomerEmail, ct))
            .RequireOwnership(inv => inv.UserId)
            .CustomAction("pay", async (invoice, ct) =>
            {
                await _billing.ChargeAsync(invoice.Id, invoice.Total, ct);
                invoice.Status = "paid";
                invoice.PaidAt = DateTimeOffset.UtcNow;
            });
    }
}
```

That is everything. You get these endpoints, fully wired to EF Core and OpenAPI:

| Method | Route | What it does |
|--------|-------|--------------|
| `GET` | `/invoices?skip=0&take=50` | Paginated list, scoped to the current user |
| `GET` | `/invoices/{id}` | Single invoice — 403 if not owner |
| `POST` | `/invoices` | Create — sets `CreatedAt`, `Status`, sends email |
| `PUT` | `/invoices/{id}` | Update — 403 if not owner |
| `DELETE` | `/invoices/{id}` | Delete — 403 if not owner |
| `POST` | `/invoices/{id}/pay` | Domain action — charges and updates status |

No repository. No service layer. No controller. No manual route naming. No repeat.

---

## Three ideas that drive everything

Head.Net is built on three observations about how .NET APIs actually work in practice.

### 1. CRUD is infrastructure, not application code

The List/Get/Create/Update/Delete cycle is the same across every entity in every project. It is transport logic — it moves data in and out of a database over HTTP. Teams that write it by hand every time are not building faster; they are paying the same tax on every sprint.

Head.Net treats CRUD as infrastructure to be generated, not authored. You describe the surface; it builds the wiring.

### 2. The hooks are where the work actually lives

The list endpoint for invoices is never the hard part. The hard part is:

- Stamping `CreatedAt` and defaulting `Status` on create
- Sending a confirmation email after the entity is saved
- Enforcing that only the owner can modify their own records
- Blocking deletes when related orders still reference the invoice

These are the rules that encode your domain knowledge. They need a clean, consistent home — one that runs at the right moment and never mixes HTTP request objects into business code.

```csharp
// Domain logic stays clean — no HttpContext, no IActionResult
.BeforeCreate((invoice, ct) =>
{
    if (invoice.Total <= 0)
        throw new InvalidOperationException("Invoice total must be positive.");

    invoice.CreatedAt = DateTimeOffset.UtcNow;
    invoice.Status = "draft";
    return ValueTask.CompletedTask;
})
.AfterCreate(async (invoice, ct) =>
    await notifications.SendAsync($"Invoice #{invoice.Id} created", ct))
.BeforeDelete(async (id, ct) =>
    await guard.EnsureNoPendingOrdersAsync(id, ct))
```

Six hooks cover the full mutation lifecycle: `BeforeCreate`, `AfterCreate`, `BeforeUpdate`, `AfterUpdate`, `BeforeDelete`, `AfterDelete`. All support async. All receive a cancellation token.

### 3. Domain actions need a home of their own

Invoices get *paid*. Documents get *archived*. Subscriptions get *paused* and *resumed*. These operations do not map naturally to `PUT /invoices/{id}` — they represent specific state transitions with specific business rules and specific side effects.

The common workarounds are to fold them into PUT requests (unclear semantics) or scatter them across unrelated controllers (no cohesion). Neither is satisfying.

Head.Net gives every domain action a first-class place on the entity surface:

```csharp
.CustomAction("pay", async (invoice, ct) =>
{
    // The entity is already loaded — no fetch needed
    await _billing.ChargeAsync(invoice.Id, invoice.Total, ct);
    invoice.Status = "paid";
    invoice.PaidAt = DateTimeOffset.UtcNow;
    // Changes are saved automatically after the action runs
})
.CustomAction("archive", (invoice, _) =>
{
    invoice.Status = "archived";
    return Task.CompletedTask;
})
.CustomAction("void", async (invoice, ct) =>
{
    await _billing.RefundAsync(invoice.Id, ct);
    invoice.Status = "voided";
})
```

Each action routes predictably, names itself in OpenAPI, and composes naturally with hooks and authorization.

---

## Keeping configuration organized

For entities with real behavior, a long fluent chain in `Program.cs` gets unwieldy fast. Head.Net supports `IHeadEntitySetup<TEntity>` — the same pattern EF Core uses for `IEntityTypeConfiguration<T>` — to move all entity configuration into a dedicated class.

```csharp title="Program.cs — four lines regardless of entity complexity"
app.MapEntity<Invoice>()
    .WithCrud()
    .Setup<InvoiceSetup>()
    .Build();
```

The setup class is instantiated by `ActivatorUtilities`, which means its constructor parameters are resolved from DI automatically — no registration of `InvoiceSetup` itself required.

```csharp title="InvoiceSetup.cs — all domain config in one place, testable in isolation"
public sealed class InvoiceSetup : IHeadEntitySetup<Invoice>
{
    private readonly IBillingService _billing;
    private readonly ILogger<InvoiceSetup> _logger;

    public InvoiceSetup(IBillingService billing, ILogger<InvoiceSetup> logger)
    {
        _billing = billing;
        _logger = logger;
    }

    public void Configure(HeadEntityEndpointBuilder<Invoice> builder) { ... }
}
```

This is the same mental model as EF Core configuration classes. If you already organize your EF Core setup this way, the pattern is immediately familiar.

---

## What about trust?

Senior .NET developers are right to be skeptical of "magic" libraries. The .NET ecosystem has accumulated painful experiences with frameworks that hide too much, generate code that can't be debugged, or break in production in ways that are impossible to diagnose.

Head.Net is designed with this skepticism in mind.

**Every generated endpoint is a standard Minimal API lambda.** The pattern `group.MapGet("/{id:int}", async (int id, IHeadEntityStore<TEntity> store, ...) => ...)` is exactly what you would write yourself. There is no bytecode manipulation, no compile-time generation magic, and no invisible middleware.

**Every hook is a plain delegate.** You pass in a lambda or a method reference. Head.Net calls it at the right moment. You can set a breakpoint in it, test it in isolation, and reason about it without looking at the SDK source.

**The store contract is explicit.** `IHeadEntityStore<TEntity>` is a five-method interface you can read, implement, or swap out. EF Core implementation is the default, but the endpoint builder has no EF Core dependency — it only knows about the store interface.

**Escape hatches exist by design.** Anything Head.Net cannot express, you handle with standard Minimal API registration alongside it:

```csharp
// Head.Net for the standard surface
app.MapEntity<Invoice>().WithCrud().Setup<InvoiceSetup>().Build();

// Standard Minimal API for anything custom
app.MapGet("/invoices/summary", async (AppDbContext db) =>
    await db.Invoices.GroupBy(i => i.Status)
        .Select(g => new { Status = g.Key, Count = g.Count() })
        .ToListAsync());
```

Head.Net participates in the router. It does not own it.

---

## Where it fits

|  | Raw Minimal APIs | MVC Controllers | Head.Net |
|---|---|---|---|
| Boilerplate per entity | High | Very high | Near zero |
| Lifecycle hooks | Manual | Manual | Built-in |
| Domain actions | Manual | Separate controller | First-class |
| OpenAPI | Manual | Attribute-based | Automatic |
| EF Core wiring | Manual | Manual | Conventions-first |
| DI constructor injection | Full | Full | Full (setup classes) |
| Escape hatches | Full | Full | Full |
| Best suited for | Custom APIs | Complex routing | Internal APIs, admin backends, MVPs |

Head.Net does not try to be the right tool for every API. It is the right tool for the pattern that repeats: entity-centered, EF Core-backed, CRUD-heavy with domain logic layered on top.

---

## Requirements

- **.NET 8** or **.NET 9**
- **EF Core 9** (other stores can be plugged in via `IHeadEntityStore<T>`)
- **ASP.NET Core** with Minimal APIs (not MVC)
- Entities must implement `IHeadEntity<int>`

---

:::tip[Best place to start]
If you want to understand the shape of the API before installing anything, read the [Quick Example](./getting-started/quick-example). It shows a complete setup — entity, DbContext, setup class, Program.cs, and the HTTP table — in a single page.

If you are ready to add it to a project, go to [Installation](./getting-started/installation).
:::
