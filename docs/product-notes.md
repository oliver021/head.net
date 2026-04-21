# Head.Net Product Notes

## Core Understanding

Head.Net is not trying to be another general-purpose web framework, another Clean Architecture template, or just a set of helper extensions around EF Core.

The real idea is narrower and stronger:

- entities have a predictable API surface
- CRUD is mostly repetitive HTTP plumbing
- the valuable customization points are the hooks between the CRUD steps
- domain actions should feel like first-class behavior on top of that surface

That is the gap Head.Net is trying to fill.

## Ecosystem Position

Compared with adjacent options:

- raw Minimal APIs still require writing every endpoint and response pattern by hand
- MVC controllers are verbose and not especially composable
- FastEndpoints is strong for custom endpoint design, but still treats each endpoint as a separate unit

The Head.Net thesis is different:

- declare the entity
- describe the allowed surface
- attach hooks and domain actions
- let the SDK provide the HTTP plumbing and OpenAPI shape

## Product Direction

The default direction should be:

- target `.NET 8` and `.NET 9`
- build around Minimal APIs, not MVC controllers
- make the main public shape fluent and composable
- keep EF Core as the persistence center of gravity
- prioritize automatic OpenAPI as part of the product promise

## Desired Developer Experience

This is the type of programming model Head.Net should aspire to support:

```csharp
app.MapEntity<Invoice>()
   .WithCrud(options)
   .Relationship(x => x.Post)
   .BeforeCreate(inv => inv.CreatedAt = DateTime.UtcNow)
   .AfterCreate(inv => emailService.SendConfirmation(inv))
   .BeforeUpdate(ValidateOwnership)
   .AfterDelete(xHandler)
   .CustomAction("pay", async (inv, req) => await billing.Charge(inv, req))
   .CustomAction("void", async (inv, _) => await billing.Void(inv));
```

The important part is not the exact method names. The important part is the model:

- fluent entity registration
- default CRUD surface
- named domain actions
- lifecycle hooks
- relationship routes
- authorization and ownership rules
- OpenAPI discoverability by default

## Important Feature Ideas

### Custom Actions

Custom actions should feel native to the entity surface.

Examples:

- `pay` on `Invoice`
- `void` on `Invoice`
- future batch or approval actions on other entities

Desirable behavior:

- HTTP method semantics are explicit or convention-driven
- route shape is predictable, such as `/invoices/{id}/pay`
- action names appear clearly in OpenAPI

### Hook Pipeline

Hooks are likely the most important abstraction in the system.

They should support:

- async execution
- cancellation tokens
- pre- and post-operation behavior
- short-circuiting without leaking HTTP concerns into business rules

Example direction:

- `BeforeCreate`
- `AfterCreate`
- `BeforeUpdate`
- `AfterDelete`

The ideal outcome is that domain code returns rich results or validation outcomes, and the transport layer maps those into HTTP responses cleanly.

### Authorization

Authorization should not be an afterthought or only an attribute story.

It should be possible to express rules close to the entity surface, for example:

```csharp
.RequireOwnership(inv => inv.UserId)
```

This fits the product better than scattering authorization details across controller classes or endpoint handlers.

### Relationships

Entity relationships are a strong candidate for generated nested routes and navigation helpers.

This should be explored carefully because it adds power quickly, but it can also increase complexity in routing, query shape, and OpenAPI output.

## Technical Challenges

### Why this space is still open

There are structural reasons this idea has not dominated the .NET ecosystem:

- EF Core keeps improving, which reduces the need for some abstractions
- the .NET community is skeptical of hidden magic
- generic repository abstractions have accumulated negative baggage
- a library deeply coupled to EF Core has a real maintenance burden

These are not reasons to avoid the project. They are reasons to be deliberate about trust, diagnostics, and scope.

### Trust Problem

Head.Net must avoid feeling magical in a way that makes senior .NET developers nervous.

That means:

- generated behavior must be understandable
- conventions must be documented
- there must be clear escape hatches
- diagnostics should explain what the SDK mapped and why

### Source Generators

Source generation is a strong long-term direction because it helps with:

- trust
- visibility
- AOT compatibility
- reducing reflection-heavy behavior

It does not need to be the first implementation step, but it should remain a serious design target.

### AutoDbContext

Automatic DbContext support can be valuable, but only if it is clearly bounded.

It should always provide:

- explicit assumptions
- a documented `ModelBuilder` extension point
- a clear path back to manual control
