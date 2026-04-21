# Head.Net.EntityFrameworkCore

Entity Framework Core integration for Head.Net.

## What is it?

Provides the `IHeadEntityStore<TEntity, TKey>` implementation using Entity Framework Core, handling entity tracking, change detection, and persistence.

## Installation

```bash
dotnet add package Head.Net.EntityFrameworkCore
```

## Basic Usage

Register the store with your DbContext:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseInMemoryDatabase("mydb"));

builder.Services.AddHeadEntityStore<AppDbContext, Product>();

var app = builder.Build();
// ... use with Head.Net.AspNetCore
```

## Supported Key Types

Works with any primary key type implementing `IEquatable<TKey>` and `IComparable<TKey>`:

- `int`
- `Guid`
- `long`
- `string`
- Custom types (must implement the interfaces)

## Documentation

See the [main repository](https://github.com/oliver021/head.net) for full guides and examples.
