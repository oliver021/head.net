---
description: Head.Net API guide — wire up CRUD, hooks, validation, custom actions, and authorization for $ARGUMENTS
allowed-tools: Read, Glob, Grep, Edit, Write, Bash
---

You are helping the user work with the Head.Net SDK. The full skill reference is at `skills/head-net-api.md` — read it first, then act on the user's specific request.

User request: $ARGUMENTS

## How to proceed

1. **Read `skills/head-net-api.md`** — understand the full API, patterns, and decision tree before writing anything.
2. **Read the current codebase state:**
   - Glob `src/**/*.cs` to find existing entities and DbContext
   - Read `samples/Head.Net.SampleApi/Program.cs` to see how the application is wired
   - Grep for `IHeadEntity` to see existing entity definitions
3. **Identify which steps in the skill apply** to the user's request (the skill has a "Common user requests → which steps apply" table).
4. **Generate or modify only what is needed** — don't add unrelated setup, don't over-engineer.
5. **Run `dotnet build Head.Net.sln`** after changes and fix any errors before reporting done.
6. **Run `dotnet test Head.Net.sln`** and confirm all tests pass.
7. **Report what was added and where** — file paths and what each change does.
