# Head.Net.AspNetCore

ASP.NET Core integration for Head.Net — generates Minimal API endpoints from entity definitions.

## What is it?

Automatically generates CRUD endpoints (GET, POST, PUT, DELETE) for your entities, with support for lifecycle hooks, validation, authorization, custom actions, and paging.

## Installation

```bash
dotnet add package Head.Net.AspNetCore
```

## Basic Usage

Define your entity and wire it up in `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddHeadEntityStore<AppDbContext, Product>();

var app = builder.Build();

// Map CRUD endpoints
app.MapEntity<Product>()
    .WithCrud()
    .BeforeCreate((p, _) => 
    {
        p.CreatedAt = DateTimeOffset.UtcNow;
        return new ValueTask<HeadHookResult<Product>?>((HeadHookResult<Product>?)null);
    })
    .Build();

app.Run();
```

## Features

- **CRUD Endpoints** — Automatic routing and request/response handling
- **Lifecycle Hooks** — BeforeCreate, AfterCreate, BeforeUpdate, AfterUpdate, BeforeDelete, AfterDelete
- **Validation** — Short-circuit validation with 400 Problem Details responses
- **Authorization** — Ownership checks and custom policies
- **Custom Actions** — Domain-specific routes like `/products/{id}/archive`
- **Paging** — Configurable list pagination
- **Error Handling** — RFC 7807 Problem Details for all errors
- **Setup Classes** — Organize configuration in dedicated classes with dependency injection

## Documentation

See the [main repository](https://github.com/oliver021/head.net) for full guides and examples.
