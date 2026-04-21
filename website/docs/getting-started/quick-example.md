---
sidebar_position: 3
title: Quick Example
description: A complete end-to-end Head.Net setup with an Invoice entity.
---

import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

# Quick Example

This example walks through a complete working setup: an `Invoice` entity with CRUD endpoints, lifecycle hooks, a custom domain action, and paging — organized using a setup class.

## The entity

```csharp
using Head.Net.Abstractions;

public sealed class Invoice : IHeadEntity<int>
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Status { get; set; } = "draft";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
}
```

## The DbContext

```csharp
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Invoice> Invoices => Set<Invoice>();
}
```

## The setup class

The setup class centralizes all entity-specific configuration: hooks, custom actions, paging, and authorization. It can accept dependencies through constructor injection — no DI registration required.

```csharp
using Head.Net.AspNetCore;

public sealed class InvoiceSetup : IHeadEntitySetup<Invoice>
{
    // Optional: inject services through the constructor
    // private readonly ILogger<InvoiceSetup> _logger;
    // public InvoiceSetup(ILogger<InvoiceSetup> logger) => _logger = logger;

    public void Configure(HeadEntityEndpointBuilder<Invoice> builder)
    {
        builder
            .WithPaging(enable: true, defaultPageSize: 50)

            // Set timestamps and defaults on create
            .BeforeCreate((invoice, _) =>
            {
                invoice.CreatedAt = DateTimeOffset.UtcNow;
                invoice.Status = "draft";
                return ValueTask.CompletedTask;
            })

            // Domain action: mark an invoice as paid
            .CustomAction("pay", (invoice, _) =>
            {
                invoice.Status = "paid";
                invoice.PaidAt = DateTimeOffset.UtcNow;
                return Task.CompletedTask;
            });
    }
}
```

## Program.cs

<Tabs>
  <TabItem value="setup-class" label="With Setup Class (Recommended)" default>

```csharp
using Head.Net.AspNetCore;
using Head.Net.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseInMemoryDatabase("invoices"));

builder.Services.AddHeadEntityStore<AppDbContext, Invoice>();

var app = builder.Build();

app.MapEntity<Invoice>()
    .WithCrud()
    .Setup<InvoiceSetup>()
    .Build();

app.Run();
```

  </TabItem>
  <TabItem value="inline" label="Inline (Simple entities)">

```csharp
var app = builder.Build();

app.MapEntity<Invoice>()
    .WithCrud()
    .WithPaging(enable: true, defaultPageSize: 50)
    .BeforeCreate((invoice, _) =>
    {
        invoice.CreatedAt = DateTimeOffset.UtcNow;
        invoice.Status = "draft";
        return ValueTask.CompletedTask;
    })
    .CustomAction("pay", (invoice, _) =>
    {
        invoice.Status = "paid";
        invoice.PaidAt = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    })
    .Build();

app.Run();
```

  </TabItem>
</Tabs>

## What you get

Running this application gives you:

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/invoices` | List all invoices (paged, `?skip=0&take=50`) |
| `GET` | `/invoices/{id}` | Get a single invoice |
| `POST` | `/invoices` | Create an invoice (BeforeCreate sets `CreatedAt` and `Status`) |
| `PUT` | `/invoices/{id}` | Update an invoice |
| `DELETE` | `/invoices/{id}` | Delete an invoice |
| `POST` | `/invoices/{id}/pay` | Custom action: mark as paid |

All endpoints are registered as standard Minimal API handlers and appear in OpenAPI automatically.

## The list response

The list endpoint returns a paginated result:

```json
{
  "data": [
    {
      "id": 1,
      "customerName": "Northwind",
      "total": 149.95,
      "status": "draft",
      "createdAt": "2024-04-20T10:00:00Z",
      "paidAt": null
    }
  ],
  "totalCount": 1,
  "skip": 0,
  "take": 50,
  "pageCount": 1,
  "pageNumber": 0
}
```

## Calling the pay action

```http
POST /invoices/1/pay
```

The handler fetches invoice 1, calls the `pay` action delegate, persists the changes, and returns the updated invoice:

```json
{
  "id": 1,
  "customerName": "Northwind",
  "total": 149.95,
  "status": "paid",
  "createdAt": "2024-04-20T10:00:00Z",
  "paidAt": "2024-04-20T11:30:00Z"
}
```

---

## Next steps

- Add more lifecycle hooks — see [Entity Lifecycle Hooks](../guides/entity-lifecycle-hooks)
- Add more custom actions — see [Custom Actions](../guides/custom-actions)
- Protect endpoints with ownership rules — see [Authorization & Ownership](../guides/authorization-and-ownership)
- Learn why the SDK is designed this way — see [Development Philosophy](./development-philosophy)
