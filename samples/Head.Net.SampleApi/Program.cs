using Head.Net.Abstractions;
using Head.Net.AspNetCore;
using Head.Net.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SampleDbContext>(options => options.UseInMemoryDatabase("head-net-sample"));
builder.Services.AddHeadEntityStore<SampleDbContext, Invoice>();

var app = builder.Build();

await SeedAsync(app.Services);

app.MapGet("/", () => Results.Ok(new
{
    Name = "Head.Net Sample API",
    Status = "Phases 1-3: CRUD + Hooks + Query + Custom Actions",
    Endpoints = new[]
    {
        "GET /invoices - List with paging (?skip=0&take=10)",
        "GET /invoices/{id} - Get invoice",
        "POST /invoices - Create invoice (sets CreatedAt, Status='draft')",
        "PUT /invoices/{id} - Update invoice (validates total)",
        "DELETE /invoices/{id} - Delete invoice (audit logs before delete)",
        "POST /invoices/{id}/pay - Custom action to mark paid",
        "POST /invoices/{id}/archive - Custom action to archive",
    }
}));

app.MapEntity<Invoice>()
    .WithCrud()
    .Setup<InvoiceSetup>()
    .Build();

app.Run();

static async Task SeedAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<SampleDbContext>();

    await dbContext.Database.EnsureCreatedAsync();

    if (await dbContext.Invoices.AnyAsync())
    {
        return;
    }

    dbContext.Invoices.AddRange(
        new Invoice
        {
            CustomerName = "Northwind",
            Total = 149.95m,
            Status = "draft",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
        },
        new Invoice
        {
            CustomerName = "Contoso",
            Total = 299.50m,
            Status = "paid",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-5),
            PaidAt = DateTimeOffset.UtcNow.AddDays(-3),
        },
        new Invoice
        {
            CustomerName = "Adventure Works",
            Total = 75.00m,
            Status = "draft",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
        }
    );

    await dbContext.SaveChangesAsync();
}

/// <summary>
/// Sample invoice entity used to demonstrate Head.Net CRUD, hooks, and custom actions.
/// </summary>
public sealed class Invoice : IHeadEntity<int>
{
    /// <summary>
    /// Gets or sets the invoice identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the invoice total.
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// Gets or sets the invoice status (draft, paid, archived).
    /// </summary>
    public string Status { get; set; } = "draft";

    /// <summary>
    /// Gets or sets when the invoice was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the invoice was paid.
    /// </summary>
    public DateTimeOffset? PaidAt { get; set; }
}

/// <summary>
/// EF Core context used by the sample API.
/// </summary>
public sealed class SampleDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SampleDbContext"/> class.
    /// </summary>
    public SampleDbContext(DbContextOptions<SampleDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the sample invoice set.
    /// </summary>
    public DbSet<Invoice> Invoices => Set<Invoice>();
}
