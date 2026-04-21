---
sidebar_position: 1
title: Installation
description: Add the Head.Net packages to your .NET project.
---

# Installation

Head.Net is distributed as three focused NuGet packages. You only install what you need.

## Packages

| Package | Purpose |
|---|---|
| `Head.Net.Abstractions` | Core interfaces and contracts. Rarely needed directly. |
| `Head.Net.EntityFrameworkCore` | EF Core store implementation and DI registration. |
| `Head.Net.AspNetCore` | Minimal API endpoint generation and the fluent builder. |

For most projects you install both the EF Core and ASP.NET Core packages. `Abstractions` is pulled in automatically as a transitive dependency.

## .NET CLI

```bash
dotnet add package Head.Net.EntityFrameworkCore
dotnet add package Head.Net.AspNetCore
```

## Package Manager

```powershell
Install-Package Head.Net.EntityFrameworkCore
Install-Package Head.Net.AspNetCore
```

## PackageReference

```xml
<ItemGroup>
  <PackageReference Include="Head.Net.EntityFrameworkCore" Version="*" />
  <PackageReference Include="Head.Net.AspNetCore" Version="*" />
</ItemGroup>
```

## Target frameworks

All packages target **net8.0** and **net9.0**. There is no support for older .NET versions or .NET Framework.

## What each package brings in

**Head.Net.EntityFrameworkCore** depends on:
- `Microsoft.EntityFrameworkCore` (9.0.x)
- `Microsoft.Extensions.DependencyInjection.Abstractions`

**Head.Net.AspNetCore** depends on:
- `Microsoft.AspNetCore.App` (shared framework, no explicit version)

Neither package pulls in a specific database provider. You choose your own EF Core provider (SQL Server, SQLite, PostgreSQL, etc.) separately.

---

## If you only need a custom store

If you are not using EF Core and want to implement your own `IHeadEntityStore<T>`, install only:

```bash
dotnet add package Head.Net.Abstractions
dotnet add package Head.Net.AspNetCore
```

Implement `IHeadEntityStore<TEntity>` directly and register it in your DI container. The endpoint builder has no dependency on EF Core.

---

Next: [Configuration](./configuration)
