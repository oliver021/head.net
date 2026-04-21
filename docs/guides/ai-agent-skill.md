---
sidebar_position: 6
title: AI Agent Skill (/head-net)
description: Use the /head-net Claude Code skill to add entities, hooks, validation, custom actions, and authorization with AI assistance.
---

# AI Agent Skill — `/head-net`

Head.Net ships a [Claude Code](https://claude.ai/code) skill that teaches an AI agent the full API surface. When invoked, the agent reads the skill reference, inspects the current codebase, and generates only the code needed to fulfil the request.

## Invoking the skill

In a Claude Code session inside the project, type:

```
/head-net Add CRUD for a Product entity with a BeforeCreate hook that stamps CreatedAt
```

```
/head-net Add a "publish" custom action to the Article entity
```

```
/head-net Protect Invoice endpoints with ownership — only the creator can read or modify
```

The argument after `/head-net` is the natural-language request. The agent handles the rest.

## What the skill covers

The skill guides the agent through every layer of the Head.Net API:

| Request | What the agent does |
|---------|-------------------|
| "Add CRUD for X" | Defines entity, registers store, maps endpoints |
| "Add a BeforeCreate hook" | Wires `BeforeCreate` with correct `HeadHookResult<T>?` return |
| "Validate data before saving" | Adds short-circuit validation with `400 Validation Failed` response |
| "Add a custom action (pay / approve / archive)" | Adds `CustomAction` with the right route |
| "Restrict access to owners" | Wires `RequireOwnership` with correct property extractor |
| "Role-based access" | Wires `RequireAuthorization` with async policy delegate |
| "Organize into a setup class" | Generates `IHeadEntitySetup<TEntity, TKey>` with DI injection |
| "Add paging" | Wires `WithPaging` with page size |
| "Filter list to current user" | Wires `WithQueryFilter` scoped to user ID |
| "Add audit logging on delete" | Wires `BeforeDelete` or `AfterDelete` |

## Skill files

The skill lives in two places in the repository:

| File | Purpose |
|------|---------|
| [`skills/head-net-api.md`](../../skills/head-net-api.md) | **Full skill reference** — API patterns, code templates, decision tree, checklist. Human-readable. |
| [`.claude/commands/head-net.md`](../../.claude/commands/head-net.md) | **Claude Code command** — invoked by `/head-net`. Reads the skill reference and acts on the user's request. |

## How the skill works

When you type `/head-net <request>`, the agent:

1. **Reads `skills/head-net-api.md`** — loads the full API reference and decision tree
2. **Inspects the codebase** — reads `Program.cs`, the DbContext, and existing entities
3. **Identifies which steps apply** — the skill maps common requests to specific steps (entity definition, store registration, endpoint mapping, hooks, actions, auth)
4. **Generates only what's needed** — no over-engineering; each step is applied only if required by the request
5. **Runs `dotnet build`** after changes and fixes any errors
6. **Runs `dotnet test`** and confirms all tests pass
7. **Reports what was added and where**

## Key patterns the skill enforces

### Hook return types

`BeforeCreate` and `BeforeUpdate` must return `ValueTask<HeadHookResult<TEntity>?>`. The skill always generates the correct form:

```csharp
// Success — proceed with save
return new ValueTask<HeadHookResult<Invoice>?>((HeadHookResult<Invoice>?)null);

// Validation failure — abort with 400
var validation = HeadValidationResult.Failure("Total must be positive");
return new ValueTask<HeadHookResult<Invoice>?>(HeadHookResult<Invoice>.Invalid(validation));
```

### Setup class interface

The skill always generates the two-parameter form:

```csharp
public sealed class ProductSetup : IHeadEntitySetup<Product, int>
{
    public void Configure(HeadEntityEndpointBuilder<Product, int> builder) { ... }
}
```

### Generic key types

The skill supports any `TKey` type. For non-int keys, it generates the explicit form:

```csharp
// Guid primary key
builder.Services.AddHeadEntityStore<AppDbContext, Order, Guid>();
app.MapEntity<Order, Guid>().WithCrud().Build();
```

## Updating the skill

The skill is a Markdown file — edit it directly to reflect changes to the SDK:

- **New hook?** Add a row to the hooks table in Step 4.
- **New builder method?** Add an entry to Step 8 or the relevant section.
- **New error code?** Add it to the error response format section.
- **New common request pattern?** Add a row to the "Common user requests" table.

The command file (`.claude/commands/head-net.md`) only needs updating if the skill file location moves or the agent's initial instructions change.
