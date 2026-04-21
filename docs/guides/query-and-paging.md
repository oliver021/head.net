---
sidebar_position: 3
title: Query & Paging
description: Control how the list endpoint returns data — paging, filtering, and result shape.
---

# Query & Paging

The list endpoint (`GET /entities`) in Head.Net returns a paginated result by default. This page explains how paging works, how to configure it, and how to apply custom query filters.

## Default behavior

Without any configuration, the list endpoint accepts `skip` and `take` query parameters with defaults of `0` and `100`:

```
GET /invoices
GET /invoices?skip=0&take=10
GET /invoices?skip=20&take=10
```

## Response shape

The list endpoint always returns a `HeadPagedResult<TEntity>`:

```json
{
  "data": [...],
  "totalCount": 250,
  "skip": 20,
  "take": 10,
  "pageCount": 25,
  "pageNumber": 2
}
```

| Field | Description |
|---|---|
| `data` | The items in this page |
| `totalCount` | Total items matching the query (before skip/take) |
| `skip` | Items skipped |
| `take` | Page size requested |
| `pageCount` | Total number of pages (`ceil(totalCount / take)`) |
| `pageNumber` | Current page (0-indexed, `floor(skip / take)`) |

## Configuring paging

Use `WithPaging` to change the default page size or disable paging:

```csharp
app.MapEntity<Invoice>()
    .WithCrud()
    .WithPaging(enable: true, defaultPageSize: 25)
    .Build();
```

The `take` parameter is capped at `defaultPageSize`. A client requesting `?take=500` with a configured `defaultPageSize: 25` will receive at most 25 items.

### Disabling paging

```csharp
.WithPaging(enable: false)
```

When paging is disabled, all items are returned in the `data` array. The `totalCount`, `skip`, `take`, `pageCount`, and `pageNumber` fields still appear in the response for consistency.

## Custom query filters

`WithQueryFilter` applies a fixed filter to all list requests. Use it to exclude soft-deleted records, scope results to a tenant, or enforce access rules at the query level.

```csharp
.WithQueryFilter(q => q.Where(inv => inv.Status != "archived"))
```

The filter receives an `IQueryable<TEntity>` and returns a filtered `IQueryable<TEntity>`. It runs before the skip/take pagination, so `totalCount` reflects the filtered count.

### Combining multiple filters

Chain multiple `Where` clauses in a single filter:

```csharp
.WithQueryFilter(q => q
    .Where(inv => inv.Status != "archived")
    .Where(inv => inv.Total > 0))
```

Or compose filters from a service:

```csharp
public sealed class InvoiceSetup : IHeadEntitySetup<Invoice>
{
    private readonly ITenantContext _tenant;

    public InvoiceSetup(ITenantContext tenant) => _tenant = tenant;

    public void Configure(HeadEntityEndpointBuilder<Invoice> builder)
    {
        builder.WithQueryFilter(q => q.Where(inv => inv.TenantId == _tenant.Id));
    }
}
```

The `ITenantContext` is resolved from DI through the setup class constructor.

## HeadQueryOptions

`HeadQueryOptions` is the model for the query parameters. It is available in `Head.Net.Abstractions` if you need to work with it directly:

```csharp
var options = new HeadQueryOptions
{
    Skip = 0,
    Take = 25,
    OrderBy = "Total,-CreatedAt",  // sort by Total asc, then CreatedAt desc
};
```

:::info
Column-level filtering (e.g., `?status=paid`) and explicit sorting via the `OrderBy` parameter are on the roadmap. `HeadQueryOptions` is already structured to support them. For now, use `WithQueryFilter` for fixed filters.
:::

## Sorting

Server-side sorting by request parameter is not yet implemented. As a workaround, apply a default sort in `WithQueryFilter`:

```csharp
.WithQueryFilter(q => q.OrderByDescending(inv => inv.CreatedAt))
```

This ensures a consistent order on every list request regardless of client parameters.

## Client-side pagination patterns

For offset-based pagination (most common):

```
Page 1: GET /invoices?skip=0&take=20
Page 2: GET /invoices?skip=20&take=20
Page 3: GET /invoices?skip=40&take=20
```

Calculate the current page from the response:

```json
{
  "pageNumber": 0,
  "pageCount": 13,
  "totalCount": 250
}
```

For a "load more" / infinite scroll pattern, use the `skip` value from your last request plus the number of items returned.
