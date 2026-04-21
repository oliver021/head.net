# Head.Net Roadmap

## Planning Goal

Use this roadmap to move from product thesis to a credible first vertical slice without overcommitting the architecture too early.

## Phase 0: Product Definition

Capture the product boundaries before writing more framework code.

Deliverables:

- a stable statement of what Head.Net is and is not
- a first-pass public API direction
- a list of explicit non-goals for v1

Key questions:

- Is v1 Minimal API only?
- Is v1 CRUD-first with custom actions, or CRUD only?
- Does v1 expose entities directly, or support DTO shaping immediately?
- How much query capability belongs in the first release?

Exit criteria:

- the public story can be explained in a short README section
- the team can describe the first vertical slice without ambiguity

## Phase 1: Metadata Model

Define the metadata and fluent API that describe an entity surface.

Focus areas:

- entity registration
- CRUD operation flags
- hook registration
- custom action definition
- authorization hooks
- relationship metadata

Suggested outcome:

- the `Head.Net.Abstractions` package contains the contracts and configuration model
- the intended fluent API is sketched, even if not fully implemented

Exit criteria:

- there is one clear model for how an entity becomes an API surface
- the abstractions feel small, readable, and extensible

## Phase 2: Minimal Vertical Slice

Implement a thin end-to-end path for one entity in one sample application.

Recommended slice:

- `MapEntity<TEntity>()`
- generated CRUD endpoints
- one `BeforeCreate` hook
- one `CustomAction`
- basic OpenAPI output

Keep this slice deliberately small. The goal is not feature breadth. The goal is proving that the programming model feels better than writing endpoints by hand.

Exit criteria:

- one sample entity can be mapped with minimal code
- CRUD routes are discoverable
- one custom action works cleanly
- the OpenAPI output is readable

## Phase 3: Query and Policy Layer

Add the first useful non-trivial behaviors around the CRUD surface.

Focus areas:

- filtering
- sorting
- paging
- ownership or authorization rules
- result shaping and validation short-circuiting

This is where Head.Net starts earning its value beyond "endpoint generation."

Exit criteria:

- common list endpoints support a predictable query experience
- authorization or ownership can be expressed fluently
- the hook pipeline can short-circuit cleanly

## Phase 4: Trust and Diagnostics

Reduce the "magic" risk.

Focus areas:

- diagnostics for mapped entities and generated routes
- documentation of conventions
- explicit override hooks
- evaluation of source generation for visibility and AOT readiness

Exit criteria:

- users can understand what Head.Net generated
- developers have a documented way to override or opt out
- there is a clear decision on runtime generation versus source generation

## Phase 5: Packaging and Publishing

Make the repo ready for sustainable NuGet delivery.

Focus areas:

- package metadata quality
- versioning strategy
- sample and documentation quality
- release workflow

Exit criteria:

- package boundaries are stable enough for public publishing
- at least one sample demonstrates the intended experience clearly
- the documentation shows how to adopt the library safely
