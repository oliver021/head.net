---
sidebar_position: 2
title: Configuration
description: Wire up Head.Net with your EF Core DbContext and ASP.NET Core application.
---

# Configuration

Head.Net wires into your application in two places: the **service registration** in `Program.cs` (or your DI setup) and the **endpoint mapping** on the app builder.

## 1. Register your DbContext

Head.Net builds on top of your existing EF Core `DbContext`. There is no separate context to configure.

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
```

Your `DbContext` should expose the entity as a `DbSet<T>`:

```csharp
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Invoice> Invoices => Set<Invoice>();
}
```

## 2. Register the Head.Net store

Call `AddHeadEntityStore` once per entity you want to expose:

```csharp
builder.Services.AddHeadEntityStore<AppDbContext, Invoice>();
```

This registers a scoped `IHeadEntityStore<Invoice>` backed by your `AppDbContext`. The store handles List, Get, Create, Update, Delete, and SaveChanges operations.

Register one call per entity:

```csharp
builder.Services.AddHeadEntityStore<AppDbContext, Invoice>();
builder.Services.AddHeadEntityStore<AppDbContext, Product>();
builder.Services.AddHeadEntityStore<AppDbContext, Customer>();
```

## 3. Ensure your entity implements IHeadEntity

Every entity exposed through Head.Net must implement `IHeadEntity<TKey>`:

```csharp
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

`IHeadEntity<int>` requires only an `Id` property. The type parameter is the key type — `int` is the convention, but you can use any comparable type.

## 4. Map endpoints

After `builder.Build()`, map your entities onto the route builder:

```csharp
var app = builder.Build();

app.MapEntity<Invoice>()
    .WithCrud()
    .Build();
```

`.WithCrud()` enables all five standard operations by default. Call it with a configure action to disable specific ones:

```csharp
app.MapEntity<Invoice>()
    .WithCrud(options =>
    {
        options.EnableDelete = false; // invoices can't be deleted
    })
    .Build();
```

## Minimal complete example

```csharp
using Head.Net.AspNetCore;
using Head.Net.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddHeadEntityStore<AppDbContext, Invoice>();

var app = builder.Build();

app.MapEntity<Invoice>().WithCrud().Build();

app.Run();
```

That's the minimum to get a fully functional CRUD API for `Invoice`.

---

## OpenAPI

Head.Net names every endpoint using the pattern `{EntityName}_{Operation}`:

- `Invoice_List`
- `Invoice_Get`
- `Invoice_Create`
- `Invoice_Update`
- `Invoice_Delete`
- `Invoice_{ActionName}` for custom actions

These names appear in the OpenAPI spec and in Swagger UI. You do not need any additional configuration to get OpenAPI output — ASP.NET Core's built-in OpenAPI support picks them up automatically.

---

Next: [Quick Example](./quick-example)
