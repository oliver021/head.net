using Head.Net.AspNetCore;

/// <summary>
/// Centralizes all Invoice endpoint configuration: hooks, custom actions, and paging.
/// Demonstrates the <see cref="IHeadEntitySetup{TEntity}"/> pattern.
/// Constructor parameters are injected from DI — uncomment the logger example below to try it.
/// </summary>
public sealed class InvoiceSetup : IHeadEntitySetup<Invoice>
{
    // Example of DI constructor injection — no registration in DI needed:
    // private readonly ILogger<InvoiceSetup> _logger;
    // public InvoiceSetup(ILogger<InvoiceSetup> logger) => _logger = logger;

    /// <inheritdoc/>
    public void Configure(HeadEntityEndpointBuilder<Invoice> builder)
    {
        builder
            .WithPaging(enable: true, defaultPageSize: 100)
            .BeforeCreate((invoice, _) =>
            {
                invoice.CreatedAt = DateTimeOffset.UtcNow;
                invoice.Status = "draft";
                return ValueTask.CompletedTask;
            })
            .AfterCreate((invoice, _) =>
            {
                Console.WriteLine($"[AfterCreate] Invoice {invoice.Id} created by {invoice.CustomerName}");
                return ValueTask.CompletedTask;
            })
            .BeforeUpdate((id, invoice, _) =>
            {
                if (invoice.Total < 0)
                    Console.WriteLine($"[BeforeUpdate] Invalid total: {invoice.Total}");
                return ValueTask.CompletedTask;
            })
            .AfterUpdate((id, invoice, _) =>
            {
                Console.WriteLine($"[AfterUpdate] Invoice {id} updated to status {invoice.Status}");
                return ValueTask.CompletedTask;
            })
            .BeforeDelete((id, _) =>
            {
                Console.WriteLine($"[BeforeDelete] About to delete invoice {id}");
                return ValueTask.CompletedTask;
            })
            .AfterDelete((invoice, _) =>
            {
                Console.WriteLine($"[AfterDelete] Invoice {invoice.Id} deleted (was {invoice.Status})");
                return ValueTask.CompletedTask;
            })
            .CustomAction("pay", (invoice, _) =>
            {
                invoice.Status = "paid";
                invoice.PaidAt = DateTimeOffset.UtcNow;
                Console.WriteLine($"[CustomAction:pay] Invoice {invoice.Id} marked paid");
                return Task.CompletedTask;
            })
            .CustomAction("archive", (invoice, _) =>
            {
                invoice.Status = "archived";
                Console.WriteLine($"[CustomAction:archive] Invoice {invoice.Id} archived");
                return Task.CompletedTask;
            });
    }
}
