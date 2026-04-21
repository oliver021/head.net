# Head.Net.Abstractions

Core abstractions and contracts for the Head.Net SDK.

## What is it?

Shared interfaces and types that define the Head.Net API surface, with no external dependencies.

## Includes

- `IHeadEntity<TKey>` — Base interface for entities with a primary key
- `IHeadEntityStore<TEntity, TKey>` — CRUD contract (List, Get, Create, Update, Delete)
- `HeadCrudOptions` — Per-entity configuration (enable/disable operations)
- `HeadEntityHooks` — Lifecycle hook delegates (BeforeCreate, AfterCreate, etc.)
- `HeadValidationResult` — Validation error results
- `HeadHookResult<TEntity>` — Hook result type for validation short-circuiting
- `HeadAuthorizationResult` — Authorization policy results

## Installation

```bash
dotnet add package Head.Net.Abstractions
```

## Basic Usage

Implement `IHeadEntity<TKey>` on your entity:

```csharp
public class Product : IHeadEntity<int>
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```

Implement `IHeadEntityStore<TEntity, TKey>` or use the EF Core provider:

```csharp
services.AddHeadEntityStore<AppDbContext, Product>();
```

## Documentation

See the [main repository](https://github.com/oliver021/head.net) for full guides and examples.
