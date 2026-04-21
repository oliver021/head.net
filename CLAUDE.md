# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Design Philosophy

Head.Net fills a specific gap in the .NET ecosystem. It's not a general web framework, clean architecture template, or just EF Core helpers. Instead, it combines three insights:

1. **Entities are the center.** A well-defined entity with clear CRUD boundaries is the most natural starting point for API design.
2. **CRUD is boilerplate.** Endpoints, repository layers, controllers, DTOs, and OpenAPI wiring all exist mostly to expose CRUD operations. This is repetitive.
3. **Customization happens at hooks.** Where CRUD gets interesting is not in the plumbing but in the business logic: validation before create, notifications after delete, custom domain actions like "pay invoice" or "void charge."

**The thesis:** Declare the entity, describe the surface, attach lifecycle hooks and domain actions, and let the SDK provide the HTTP plumbing and OpenAPI shape automatically.

This sits between raw Minimal APIs (still require writing every endpoint) and generic frameworks (too much magic). Head.Net targets developers who want to feel in control but don't want to repeat the same endpoint patterns for every entity.

## Current Project State

**What's implemented (Phase 1-2):**
- Abstractions: `IHeadEntity<TKey>`, `IHeadEntityStore<TEntity, TKey>` CRUD contract, `HeadCrudOptions` per-entity configuration
- Hook validation: `BeforeCreate`/`BeforeUpdate` return `HeadHookResult<TEntity>?` for validation short-circuiting
- RFC 7807 ProblemDetails: All error responses standardized across endpoints
- Generic key type support: Entities work with int, Guid, long, string, or any IEquatable<TKey> primary key
- EF Core store: `HeadEntityDbContextStore<TContext, TEntity, TKey>` wrapping DbContext operations with full TKey support
- Minimal API wiring: `HeadEntityEndpointBuilder<TEntity, TKey>` generates CRUD endpoints from stores
- Authorization service: `HeadAuthorizationService<TEntity, TKey>` with ownership checks and custom policies
- Hooks execution service: `HeadHookExecutionService<TEntity, TKey>` managing lifecycle hooks
- Query builder service: `HeadQueryBuilderService<TEntity>` handling pagination and filtering
- Basic test coverage with multi-framework support (net8.0, net9.0) — all 85 tests passing
- Sample API demonstrating end-to-end flow with hooks and custom actions

**What's planned (Phase 3-5):**
- Query layer expansion: advanced filtering, sorting with TKey support, complex ownership rules
- Custom domain actions: routes like `/invoices/{id}/pay` wired to custom handlers (already wired with auth)
- Soft delete support: logical deletion tracking in query layer
- Audit logging: hook pipeline tracking entity change history
- Role-based authorization: advanced authorization policies beyond ownership
- Diagnostics: explain what Head.Net generated and how to override
- Source generation: move from reflection to compile-time code gen for trust and AOT

**Current roadmap focus:** Phase 3 query layer expansion to support advanced filtering, sorting, and complex authorization scenarios with full TKey support.

## Core Architecture

**Three-layer package design:**

1. **Head.Net.Abstractions** — Shared contracts (no external dependencies)
   - `IHeadEntity<TKey>`: Base interface requiring an `Id` property of type `TKey`
   - `IHeadEntityStore<TEntity, TKey>`: CRUD contract (ListAsync, GetAsync, CreateAsync, UpdateAsync, DeleteAsync, SaveChangesAsync) with generic key type
   - `HeadCrudOptions`: Per-entity configuration controlling which operations are exposed (EnableList, EnableGet, EnableCreate, EnableUpdate, EnableDelete)
   - `HeadEntityHooks`: Lifecycle hooks for custom behavior with validation short-circuiting (BeforeCreate/BeforeUpdate return `HeadHookResult<TEntity>?`)
   - `HeadValidationResult`, `HeadHookResult<T>`, `HeadAuthorizationResult`: Result types for validation, hooks, and authorization

2. **Head.Net.EntityFrameworkCore** — EF Core integration
   - `HeadEntityDbContextStore<TContext, TEntity, TKey>`: Generic store implementation wrapping DbContext operations with full TKey support
   - Handles entity tracking, change detection, and SaveChangesAsync using `TKey.Equals()` and `IComparable<TKey>` for ordering
   - Registers stores via `HeadNetEntityFrameworkServiceCollectionExtensions` with TKey overloads and backward-compatible int defaults

3. **Head.Net.AspNetCore** — Minimal API endpoint generation
   - `HeadEntityEndpointBuilder<TEntity, TKey>`: Fluent builder generating CRUD endpoints with authorization and hooks
   - 6 endpoint handlers: List, Get, Create, Update, Delete, CustomAction — all support arbitrary TKey types
   - Services: `HeadAuthorizationService<TEntity, TKey>`, `HeadHookExecutionService<TEntity, TKey>`, `HeadQueryBuilderService<TEntity>`, `HeadUserContextService`
   - `HeadErrorResponseService`: RFC 7807 ProblemDetails error formatting (NotFound, Forbidden, ValidationFailed)
   - `HeadNetEndpointRouteBuilderExtensions`: Fluent entry point with `app.MapEntity<TEntity, TKey>()` and backward-compatible `MapEntity<TEntity>()`
   - `IHeadEntitySetup<TEntity, TKey>`: Centralized endpoint configuration pattern
   - `HeadEntityActionDefinition<TEntity>`: Per-action configuration (method, route, handler)

**Data flow:** Entity class → `IHeadEntity<TKey>` → `IHeadEntityStore<TEntity, TKey>` → `HeadEntityEndpointBuilder<TEntity, TKey>` → Minimal API routes with validation, authorization, hooks, and RFC 7807 errors

**Design principles to maintain:**
- Explicit over magical: generated behavior must be understandable
- Convention first, escape hatches second: sensible defaults but always an override path
- Small and focused: each package solves one problem, not everything
- EF Core at the center: no competing ORMs or persistence patterns
- Fluent API surface: configuration reads like English, is discoverable, and feels natural

## Architecture Decisions & Trade-Offs

### Why Minimal APIs, Not MVC Controllers?
Minimal APIs align with Head.Net's philosophy: developers declare the entity once, and the SDK generates the routing and request/response wiring. MVC controllers would require writing more boilerplate per entity. Minimal APIs are also more discoverable via the fluent extension method pattern.

### Why IHeadEntityStore<TEntity, TKey> as the Core Abstraction?
The store interface is intentionally minimal (CRUD + SaveChanges) and fully generic over TKey. This keeps the abstraction stable and testable while supporting any primary key type (int, Guid, long, string, etc.). Customization happens through hooks and domain actions, not by reimplementing the store. The store is "dumb"—it just applies operations to EF Core. Business logic lives in hooks.

### Why EF Core InMemory for Tests?
InMemory allows tests to run without external database setup while still exercising the full stack: entity tracking, change detection, and relationship handling. No database means tests stay fast and deterministic.

### Why Central Package Management?
`Directory.Packages.props` ensures all packages stay in sync across the solution. This matters when releasing NuGet packages—version mismatches between Abstractions, EntityFrameworkCore, and AspNetCore would confuse users.

### Why No DTO Support Yet?
DTOs are in the roadmap but not Phase 1. Right now, entities are exposed directly. This keeps the first vertical slice focused. DTO shaping will be added in Phase 3 as the query layer expands, likely through a transformation layer in the hooks pipeline.

## Project Structure

```
src/
  Head.Net.Abstractions/          # Shared contracts, no external deps
  Head.Net.EntityFrameworkCore/   # EF Core store implementation
  Head.Net.AspNetCore/            # Minimal API endpoint generation
tests/
  Head.Net.Tests/                 # xunit integration tests (net8.0, net9.0)
samples/
  Head.Net.SampleApi/             # Runnable web host demonstrating SDK usage
website/                          # Docusaurus docs (separate Node.js build)
docs/
  product-notes.md                # Product thesis and ecosystem position
  roadmap.md                      # Planning and delivery phases
```

### Package Versioning

Centrally managed via `Directory.Packages.props`:
- Microsoft.EntityFrameworkCore: 9.0.4
- Microsoft.NET.Test.Sdk: 17.14.1
- xunit: 2.9.3
- coverlet.collector: 6.0.4

## Build Commands

### Compile
```powershell
dotnet build Head.Net.sln
```

### Run Tests
```powershell
dotnet test Head.Net.sln
```

Run a single test class:
```powershell
dotnet test Head.Net.sln --filter ClassName=HeadEntityDbContextStoreTests
```

Run tests for a specific target framework:
```powershell
dotnet test Head.Net.sln --framework net9.0
```

### Build Packages
```powershell
dotnet pack Head.Net.sln
```
Outputs to `artifacts/packages/`

### Website Build
```bash
cd website
yarn
yarn build
```

### Lint & Strict Checks
- `Directory.Build.props` enables:
  - `TreatWarningsAsErrors: true` — build fails on any warning
  - `Nullable: enable` — nullable reference types enforced
  - `ImplicitUsings: enable` — global usings from implicit imports
  - `LangVersion: latest` — latest C# version features

## Testing Strategy

- **Framework:** xunit
- **Test data:** Microsoft.EntityFrameworkCore.InMemory for isolation
- **Multi-target:** Tests run against both net8.0 and net9.0
- **Key test file:** `tests/Head.Net.Tests/HeadEntityDbContextStoreTests.cs`
- **Coverage:** Tracked via coverlet.collector

Tests should focus on:
- Store contract implementation (CRUD operations)
- EF Core integration and entity tracking
- Endpoint generation and routing

## Key Design Patterns

### Entity Definition
Entities must implement `IHeadEntity<TKey>`. Minimal example:
```csharp
public class Product : IHeadEntity<int>
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```

### Store Registration
Stores are auto-registered via dependency injection extension:
```csharp
services.AddHeadEntityFrameworkCore(dbContext);
```

### Endpoint Wiring
Endpoints are built from stores using the ASP.NET Core integration:
```csharp
app.MapHeadEntity<Product>(options => {
    options.EnableDelete = false; // Customize CRUD operations
});
```

### Multi-Framework Compatibility
- All source packages target `net8.0;net9.0`
- Test project targets both frameworks
- Use nullable reference types and latest C# syntax
- Avoid framework-specific APIs; prefer abstractions

## Development Principles

### Make Decisions by the Design Philosophy
When evaluating a feature or change, ask:
- **Does this reduce boilerplate?** If not, question whether it belongs.
- **Is this a convention or an escape hatch?** Conventions should be the happy path; escape hatches should feel intentional.
- **Could this add "magic" that confuses senior developers?** If yes, how will we make it understandable? (Diagnostics, source generation, explicit overrides)
- **Does this keep the abstraction small?** Scope creep kills focus. Defer Phase 3+ features to later phases.

### What to Prioritize
1. **Hook pipeline completion** (Phase 2): BeforeCreate, AfterCreate, BeforeUpdate, AfterDelete, BeforeDelete
2. **Custom actions** (Phase 2): routes like `app.MapHeadEntity<Invoice>().CustomAction("pay", handler)`
3. **Authorization hooks** (Phase 2-3): ownership rules fluently expressed
4. **Query layer** (Phase 3): filtering, sorting, paging on list endpoints
5. **Diagnostics** (Phase 4): explain what was generated and why
6. **Source generation** (Phase 4-5): move to compile-time code gen

What to defer:
- Relationship routes (interesting but add complexity)
- DTO auto-mapping (will come with query layer)
- Custom repository patterns (users can implement IHeadEntityStore if needed)
- GraphQL or other protocols (Minimal APIs first, always)

### Code Quality & Standards
- **Nullable:** Fully enable; use `#nullable disable` only for legacy compat
- **Analyzers:** Strict enforcement (`TreatWarningsAsErrors: true`)
- **Documentation:** XML doc comments on public members (auto-generated via `GenerateDocumentationFile`)
- **Warnings:** All warnings treated as errors—no exceptions

### When Adding Features
1. Update **Head.Net.Abstractions** first if new contracts are needed (ask: is this truly shared?)
2. Implement in **Head.Net.EntityFrameworkCore** with tests
3. Wire into **Head.Net.AspNetCore** if endpoints need generation changes
4. Always test against both net8.0 and net9.0
5. Add SampleApi example demonstrating the feature
6. Update website docs for user-facing features
7. Consider: Would source generation improve trust and AOT readiness here?

### Test Organization
- Test file names match the class being tested (e.g., `HeadEntityDbContextStoreTests.cs` for `HeadEntityDbContextStore.cs`)
- InMemory DbContext for isolation; no external database required
- Use `CancellationToken.None` for simple tests, `CancellationToken.ThrowIfCancellationRequested()` to test cancellation paths
- Test the happy path and the edge cases (missing entity, duplicate create, etc.)
- Integration tests that flow through store → endpoint builder → HTTP responses are more valuable than unit tests of individual classes

## Aspirational Fluent API

This is the developer experience Head.Net should eventually support. Method names may evolve, but the shape should feel fluent and self-documenting:

```csharp
app.MapEntity<Invoice>(options =>
{
    // CRUD surface
    options.EnableDelete = false; // or configure per-operation
})
   .Relationship(x => x.Customer)  // Phase 3: nested routes
   .BeforeCreate(inv => inv.CreatedAt = DateTime.UtcNow)
   .AfterCreate(inv => emailService.SendConfirmation(inv))
   .BeforeUpdate((id, inv) => ValidateOwnership(id, inv))
   .AfterDelete(inv => auditService.Log($"Deleted {inv.Id}"))
   .CustomAction("pay", async (inv, req) => 
   {
       await billing.Charge(inv, req.Amount);
       inv.PaidAt = DateTime.UtcNow;
       return inv;
   })
   .CustomAction("void", async (inv, _) => 
   {
       await billing.Void(inv);
       return inv;
   })
   .RequireOwnership(inv => inv.UserId);  // Phase 3: authorization
```

**Current reality:** We have basic CRUD endpoints and the structure in place to add hooks. Custom actions and authorization are the next vertical additions.

## Reference Documentation

- **README.md:** High-level project vision and layout
- **docs/product-notes.md:** Design thesis, ecosystem positioning, and feature rationale
- **docs/roadmap.md:** Five phases from product definition through packaging for NuGet
- **website/:** User-facing documentation (Docusaurus)

## When You Get Stuck

**"Is this too much magic?"**
→ Can you explain what got generated and why in 2–3 sentences? If not, it's too magical. Add diagnostics or an override hook.

**"Should we support this pattern?"**
→ Does it reduce boilerplate for the common case? Would senior .NET developers feel comfortable using it? Does it fit the roadmap phase? If no to any, defer.

**"What about performance?"**
→ Reflection and code generation have trade-offs. Phase 4 evaluates source generation. Until then, prioritize correctness and clarity. Benchmark only if there's evidence of a bottleneck.

**"Can I add a second package or layer?"**
→ Only if Abstractions, EntityFrameworkCore, or AspNetCore cannot cleanly host the feature. The three-package model is intentional. Adding a fourth should require strong justification.
