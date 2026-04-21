# Head.Net

**Convention-first EF Core and Minimal API SDK for .NET**

Head.Net reduces CRUD boilerplate in internal APIs and admin backends. Declare an entity, describe the surface, attach lifecycle hooks and custom actions — Head.Net provides the HTTP endpoints and EF Core wiring automatically.

## The Problem

You're building an internal API. There's an `Invoice` entity with business rules. Before you write domain logic, you'll spend hours on:

- Repository interfaces and implementations
- Service layers
- Controller routing
- OpenAPI annotations
- Response type declarations

All of that is infrastructure you've written a dozen times.

## The Solution

```csharp title="Program.cs"
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connString));
builder.Services.AddHeadEntityStore<AppDbContext, Invoice>();

var app = builder.Build();

app.MapEntity<Invoice>()
    .WithCrud()
    .Setup<InvoiceSetup>()
    .Build();

app.Run();
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

That's everything. You get these endpoints, fully wired to EF Core and OpenAPI:

| Method | Route | What it does |
|--------|-------|--------------|
| `GET` | `/invoices?skip=0&take=50` | Paginated list, scoped to the current user |
| `GET` | `/invoices/{id}` | Single invoice — 403 if not owner |
| `POST` | `/invoices` | Create — sets `CreatedAt`, `Status`, sends email |
| `PUT` | `/invoices/{id}` | Update — 403 if not owner |
| `DELETE` | `/invoices/{id}` | Delete — 403 if not owner |
| `POST` | `/invoices/{id}/pay` | Domain action — charges and updates status |

No repository. No service layer. No controller. No manual route naming.

## Three Core Ideas

### 1. CRUD is Infrastructure

The List/Get/Create/Update/Delete cycle is the same across every entity. Head.Net treats it as generated, not authored.

### 2. Hooks are Where the Work Lives

The list endpoint for invoices is never the hard part. The hard part is stamping timestamps, sending notifications, enforcing ownership, blocking dangerous deletes. These encode your domain knowledge.

### 3. Domain Actions Need a Home

Invoices get *paid*. Documents get *archived*. These operations don't map naturally to `PUT`. Head.Net gives them first-class routes.

## Where It Fits

|  | Raw Minimal APIs | MVC Controllers | Head.Net |
|---|---|---|---|
| Boilerplate per entity | High | Very high | Near zero |
| Lifecycle hooks | Manual | Manual | Built-in |
| Domain actions | Manual | Separate controller | First-class |
| OpenAPI | Manual | Attribute-based | Automatic |
| EF Core wiring | Manual | Manual | Conventions-first |
| Best suited for | Custom APIs | Complex routing | **Internal APIs, admin backends, MVPs** |

## Requirements

- **.NET 8** or **.NET 9**
- **EF Core 9**
- **ASP.NET Core** with Minimal APIs
- Entities implement `IHeadEntity<int>`

## Next Steps

- **[Quick Example](getting-started/quick-example.md)** — Complete end-to-end setup
- **[Installation](getting-started/installation.md)** — Add to your project
- **[Guides](guides/hooks.md)** — Learn lifecycle hooks, custom actions, authorization
