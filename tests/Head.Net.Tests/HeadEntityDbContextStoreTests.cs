using Head.Net.Abstractions;
using Head.Net.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Head.Net.Tests;

public sealed class HeadEntityDbContextStoreTests
{
    [Fact]
    public async Task CreateAsync_Persists_Entity()
    {
        await using var dbContext = CreateContext();
        var store = new HeadEntityDbContextStore<TestDbContext, TestInvoice, int>(dbContext);

        var created = await store.CreateAsync(new TestInvoice
        {
            CustomerName = "Northwind",
            Status = "draft",
            Total = 149.95m,
        }, CancellationToken.None);

        Assert.NotEqual(0, created.Id);
        Assert.Equal(1, await dbContext.Invoices.CountAsync());
    }

    [Fact]
    public async Task UpdateAsync_Updates_Matching_Entity()
    {
        await using var dbContext = CreateContext();
        var seed = new TestInvoice
        {
            CustomerName = "Northwind",
            Status = "draft",
            Total = 149.95m,
        };

        dbContext.Invoices.Add(seed);
        await dbContext.SaveChangesAsync();

        var store = new HeadEntityDbContextStore<TestDbContext, TestInvoice, int>(dbContext);
        var updated = await store.UpdateAsync(seed.Id, new TestInvoice
        {
            Id = seed.Id,
            CustomerName = "Contoso",
            Status = "paid",
            Total = 200m,
        }, CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal("Contoso", updated.CustomerName);
        Assert.Equal("paid", updated.Status);
        Assert.Equal(200m, updated.Total);
    }

    [Fact]
    public async Task SaveChangesAsync_Persists_Tracked_Custom_Action_Mutations()
    {
        await using var dbContext = CreateContext();
        var invoice = new TestInvoice
        {
            CustomerName = "Northwind",
            Status = "draft",
            Total = 149.95m,
        };

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync();

        var store = new HeadEntityDbContextStore<TestDbContext, TestInvoice, int>(dbContext);
        var loaded = await store.GetAsync(invoice.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        loaded.Status = "paid";
        await store.SaveChangesAsync(CancellationToken.None);

        var reloaded = await dbContext.Invoices.AsNoTracking().SingleAsync(x => x.Id == invoice.Id);
        Assert.Equal("paid", reloaded.Status);
    }

    [Fact]
    public async Task ListAsync_Returns_All_Entities()
    {
        await using var dbContext = CreateContext();
        dbContext.Invoices.AddRange(
            new TestInvoice { CustomerName = "A", Status = "draft", Total = 100m },
            new TestInvoice { CustomerName = "B", Status = "paid", Total = 200m },
            new TestInvoice { CustomerName = "C", Status = "draft", Total = 300m }
        );
        await dbContext.SaveChangesAsync();

        var store = new HeadEntityDbContextStore<TestDbContext, TestInvoice, int>(dbContext);
        var items = await store.ListAsync(CancellationToken.None);

        Assert.Equal(3, items.Count);
    }

    [Fact]
    public async Task DeleteAsync_Removes_Entity()
    {
        await using var dbContext = CreateContext();
        var invoice = new TestInvoice { CustomerName = "ToDelete", Status = "draft", Total = 50m };
        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync();

        var store = new HeadEntityDbContextStore<TestDbContext, TestInvoice, int>(dbContext);
        var deleted = await store.DeleteAsync(invoice.Id, CancellationToken.None);

        Assert.NotNull(deleted);
        Assert.Equal(0, await dbContext.Invoices.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_Returns_Null_For_Missing_Entity()
    {
        await using var dbContext = CreateContext();
        var store = new HeadEntityDbContextStore<TestDbContext, TestInvoice, int>(dbContext);

        var deleted = await store.DeleteAsync(999, CancellationToken.None);

        Assert.Null(deleted);
    }

    private static TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new TestDbContext(options);
    }

    public sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }

        public DbSet<TestInvoice> Invoices => Set<TestInvoice>();
    }

    public sealed class TestInvoice : IHeadEntity<int>
    {
        public int Id { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public decimal Total { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}
