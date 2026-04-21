# Head.Net

Head.Net is a fresh re-take of the EntityDock idea: a focused .NET SDK for annotating entities and getting a practical API surface with far less boilerplate.

The project is being shaped around three goals:

- reduce the CRUD tax for internal APIs and admin backends
- keep the experience cohesive across EF Core and ASP.NET Core
- publish small, composable NuGet packages that target both .NET 8 and .NET 9

## Project Statement

The .NET ecosystem still makes teams pay a repeated tax for every simple data-backed API:

DbContext setup, repository layers, service layers, controller wiring, DTO mapping, and OpenAPI glue all show up before the first useful endpoint ships.

Head.Net exists to collapse that boilerplate into a clearer, convention-friendly SDK for EF Core-first applications. The aim is not to replace every framework pattern, but to make the common path dramatically shorter for teams shipping internal tools, MVPs, and admin surfaces.

## Initial Layout

- `src/Head.Net.Abstractions` for shared contracts and attributes
- `src/Head.Net.EntityFrameworkCore` for EF Core integration
- `src/Head.Net.AspNetCore` for API generation and endpoint wiring
- `tests/Head.Net.Tests` for package and integration coverage
- `samples/Head.Net.SampleApi` for a runnable demo host

## Planning Docs

- `docs/product-notes.md` captures the product thesis, ecosystem position, and feature notes
- `docs/roadmap.md` organizes the work into planning and delivery phases

## Docs Site

- `website/` contains the Docusaurus documentation app
- the initial docs currently include a project overview and a first entity-surface example

## Build

```powershell
dotnet build Head.Net.sln
```
