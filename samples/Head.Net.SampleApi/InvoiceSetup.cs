using Head.Net.AspNetCore;

/// <summary>
/// Centralizes all Invoice endpoint configuration: hooks, custom actions, and paging.
/// Demonstrates the <see cref="IHeadEntitySetup{TEntity, TKey}"/> pattern.
/// Constructor parameters are injected from DI — uncomment the logger example below to try it.
/// </summary>
public sealed class InvoiceSetup : IHeadEntitySetup<Invoice, int>
{
    // Example of DI constructor injection — no registration in DI needed:
    // private readonly ILogger<InvoiceSetup> _logger;
    // public InvoiceSetup(ILogger<InvoiceSetup> logger) => _logger = logger;

    /// <inheritdoc/>
    public void Configure(HeadEntityEndpointBuilder<Invoice, int> builder)
    {
        builder
            .WithPaging(enable: true, defaultPageSize: 100)
            .BeforeCreate((invoice, _) =>
            {
                invoice.CreatedAt = DateTimeOffset.UtcNow;
                invoice.Status = "draft";
                return new ValueTask<Head.Net.Abstractions.HeadHookResult<Invoice>?>((Head.Net.Abstractions.HeadHookResult<Invoice>?)null); // null = success
            })
            .AfterCreate((invoice, _) =>
            {
                Console.WriteLine($"[AfterCreate] Invoice {invoice.Id} created by {invoice.CustomerName}");
                return ValueTask.CompletedTask;
            })
            .BeforeUpdate((id, invoice, _) =>
            {
                // Example: return validation error if total is negative
                if (invoice.Total < 0)
                {
                    var errors = new System.Collections.Generic.List<string> { "Total must be >= 0" };
                    var validation = Head.Net.Abstractions.HeadValidationResult.Failure(errors.ToArray());
                    var result = Head.Net.Abstractions.HeadHookResult<Invoice>.Invalid(validation);
                    return new ValueTask<Head.Net.Abstractions.HeadHookResult<Invoice>?>(result);
                }
                return new ValueTask<Head.Net.Abstractions.HeadHookResult<Invoice>?>((Head.Net.Abstractions.HeadHookResult<Invoice>?)null); // null = success
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
