---
sidebar_position: 5
title: Setup Classes
description: Organize entity configuration in dedicated classes using IHeadEntitySetup — the EF Core IEntityTypeConfiguration pattern for your API surface.
---

import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

# Setup Classes

As an entity grows in behavior — hooks before and after every operation, multiple custom actions, authorization rules, query filters — the fluent chain in `Program.cs` gets long. Long inline chains in the application entry point do not scale well: they mix infrastructure wiring with domain logic, they cannot receive injected services easily, and they push configuration away from the domain objects it configures.

Head.Net provides `IHeadEntitySetup<TEntity, TKey>` to solve this.

## The interface

```csharp
public interface IHeadEntitySetup<TEntity, TKey>
    where TEntity : class, IHeadEntity<TKey>
    where TKey : notnull, IEquatable<TKey>
{
    void Configure(HeadEntityEndpointBuilder<TEntity, TKey> builder);
}
```

For the common case of `int` primary keys, the convenience alias `IHeadEntitySetup<TEntity>` is equivalent to `IHeadEntitySetup<TEntity, int>`.

Implement it, and you have a dedicated home for everything related to one entity's API surface.

## Basic usage

<Tabs>
  <TabItem value="setup" label="Setup Class" default>

```csharp title="Program.cs"
app.MapEntity<Invoice>()
    .WithCrud()
    .Setup<InvoiceSetup>()
    .Build();
```

```csharp title="InvoiceSetup.cs"
public sealed class InvoiceSetup : IHeadEntitySetup<Invoice, int>
{
    public void Configure(HeadEntityEndpointBuilder<Invoice, int> builder)
    {
        builder
            .WithPaging(enable: true, defaultPageSize: 50)
            .BeforeCreate((invoice, _) =>
            {
                invoice.CreatedAt = DateTimeOffset.UtcNow;
                invoice.Status = "draft";
                return new ValueTask<HeadHookResult<Invoice>?>((HeadHookResult<Invoice>?)null);
            })
            .CustomAction("pay", (invoice, _) =>
            {
                invoice.Status = "paid";
                invoice.PaidAt = DateTimeOffset.UtcNow;
                return Task.CompletedTask;
            });
    }
}
```

  </TabItem>
  <TabItem value="inline" label="Inline Equivalent">

```csharp title="Program.cs"
app.MapEntity<Invoice>()
    .WithCrud()
    .WithPaging(enable: true, defaultPageSize: 50)
    .BeforeCreate((invoice, _) =>
    {
        invoice.CreatedAt = DateTimeOffset.UtcNow;
        invoice.Status = "draft";
        return ValueTask.CompletedTask;
    })
    .CustomAction("pay", (invoice, _) =>
    {
        invoice.Status = "paid";
        invoice.PaidAt = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    })
    .Build();
```

  </TabItem>
</Tabs>

## DI constructor injection

Setup classes support constructor injection without being registered in DI. Head.Net uses `ActivatorUtilities.CreateInstance<TSetup>` under the hood, which resolves constructor parameters from the application's `IServiceProvider` automatically.

```csharp
public sealed class InvoiceSetup : IHeadEntitySetup<Invoice, int>
{
    private readonly IBillingService _billing;
    private readonly ILogger<InvoiceSetup> _logger;

    // Both services are injected from DI — no registration of InvoiceSetup needed
    public InvoiceSetup(IBillingService billing, ILogger<InvoiceSetup> logger)
    {
        _billing = billing;
        _logger = logger;
    }

    public void Configure(HeadEntityEndpointBuilder<Invoice, int> builder)
    {
        builder
            .BeforeCreate(OnBeforeCreate)
            .AfterCreate(OnAfterCreate)
            .CustomAction("pay", OnPay)
            .CustomAction("void", OnVoid);
    }

    private ValueTask<HeadHookResult<Invoice>?> OnBeforeCreate(Invoice invoice, CancellationToken ct)
    {
        invoice.CreatedAt = DateTimeOffset.UtcNow;
        invoice.Status = "draft";
        _logger.LogInformation("Creating invoice for {Customer}", invoice.CustomerName);
        return new ValueTask<HeadHookResult<Invoice>?>((HeadHookResult<Invoice>?)null);
    }

    private async ValueTask OnAfterCreate(Invoice invoice, CancellationToken ct)
        => await _billing.NotifyNewInvoiceAsync(invoice.Id, ct);

    private async Task OnPay(Invoice invoice, CancellationToken ct)
    {
        await _billing.ChargeAsync(invoice.Id, invoice.Total, ct);
        invoice.Status = "paid";
        invoice.PaidAt = DateTimeOffset.UtcNow;
        _logger.LogInformation("Invoice {Id} paid", invoice.Id);
    }

    private async Task OnVoid(Invoice invoice, CancellationToken ct)
    {
        await _billing.VoidAsync(invoice.Id, ct);
        invoice.Status = "void";
        _logger.LogInformation("Invoice {Id} voided", invoice.Id);
    }
}
```

`IBillingService` and `ILogger<InvoiceSetup>` must be registered in DI normally. `InvoiceSetup` itself does not need to be registered.

## Composing with WithCrud

`Setup<TSetup>` composes with other builder calls. `WithCrud()` before `Setup<InvoiceSetup>()` applies CRUD configuration first; the setup class then adds to it.

```csharp
app.MapEntity<Invoice>()
    .WithCrud(o => o.EnableDelete = false)  // No deletes
    .Setup<InvoiceSetup>()                  // Hooks and actions
    .Build();
```

You can also call `WithCrud` from inside `Configure` if you want the setup class to own everything:

```csharp
public void Configure(HeadEntityEndpointBuilder<Invoice> builder)
{
    builder
        .WithCrud(o => o.EnableDelete = false)
        .WithPaging(true, 50)
        .BeforeCreate(OnBeforeCreate);
}

// In Program.cs — no extra calls needed
app.MapEntity<Invoice>().Setup<InvoiceSetup>().Build();
```

Both styles work. Choose the one that matches your team's preferences for where CRUD policy lives.

## When to use a setup class

Use a setup class when:
- The entity has more than two hooks
- Any hook needs an injected service
- You want to group related test coverage around one class
- Multiple entities share base configuration through a common base setup

For simple entities with one or two hooks and no services, inline configuration is fine:

```csharp
// Fine for simple cases
app.MapEntity<Tag>()
    .WithCrud()
    .BeforeCreate((tag, _) => { tag.Slug = tag.Name.ToLowerInvariant(); return ValueTask.CompletedTask; })
    .Build();
```

## Testing setup classes

Because setup classes are plain classes with constructor injection, they are straightforward to test. Inject a mock service and verify that `Configure` wires the right hooks:

```csharp
[Fact]
public async Task OnAfterCreate_Sends_Billing_Notification()
{
    var billing = new FakeBillingService();
    var setup = new InvoiceSetup(billing, NullLogger<InvoiceSetup>.Instance);

    // Invoke the hook directly using the delegate type
    var invoice = new Invoice { Id = 1, CustomerName = "Northwind", Total = 100m };
    await billing.NotifyNewInvoiceAsync(invoice.Id, CancellationToken.None);

    Assert.True(billing.NotifiedInvoiceIds.Contains(1));
}
```

Or test the full effect by constructing a `HeadEntityEndpointBuilder` in a test with a fake `IEndpointRouteBuilder` (see the SDK tests for a reference implementation).

## Pattern origin

`IHeadEntitySetup<TEntity>` follows the same shape as EF Core's `IEntityTypeConfiguration<T>`:

```csharp
// EF Core
public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasIndex(i => i.CustomerName);
        builder.Property(i => i.Status).HasMaxLength(20);
    }
}

// Head.Net
public sealed class InvoiceSetup : IHeadEntitySetup<Invoice, int>
{
    public void Configure(HeadEntityEndpointBuilder<Invoice, int> builder)
    {
        builder.WithPaging(true, 50).BeforeCreate(OnBeforeCreate);
    }
}
```

If you already use EF Core configuration classes, the mental model carries over directly.
