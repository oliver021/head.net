using Head.Net.Abstractions;
using Head.Net.AspNetCore;
using Head.Net.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Head.Net.Tests.Fixtures;

public sealed class TestWebApplicationFactory : IAsyncDisposable
{
    private readonly string _dbContextId = Guid.NewGuid().ToString("N");
    private TestServer? _testServer;
    private HttpClient? _httpClient;

    public TestHookCollector HookCollector { get; } = new();

    public TestAuthorizationProvider AuthorizationProvider { get; } = new();

    private TestDbContext GetDbContext()
    {
        if (_testServer is null) throw new InvalidOperationException("TestServer not initialized");
        var scope = _testServer.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<TestDbContext>();
    }

    public async Task InitializeAsync()
    {
        var builder = new WebHostBuilder();

        builder.ConfigureServices(services =>
        {
            services.AddRouting();

            services.AddDbContext<TestDbContext>(options =>
                options.UseInMemoryDatabase(_dbContextId)
            );

            services.AddScoped<IHeadEntityStore<TestInvoice, int>>(sp =>
                new HeadEntityDbContextStore<TestDbContext, TestInvoice, int>(
                    sp.GetRequiredService<TestDbContext>()
                )
            );

            services.AddSingleton(HookCollector);
            services.AddSingleton(AuthorizationProvider);
        });

        builder.Configure(app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapEntity<TestInvoice>("/invoices")
                    .WithCrud()
                    .BeforeCreate((entity, ct) =>
                    {
                        HookCollector.Record("BeforeCreate");
                        entity.CreatedAt = DateTime.UtcNow;
                        if (string.IsNullOrEmpty(entity.Status))
                            entity.Status = "draft";
                        return new ValueTask<HeadHookResult<TestInvoice>?>((HeadHookResult<TestInvoice>?)null); // Success; proceed
                    })
                    .AfterCreate((entity, ct) =>
                    {
                        HookCollector.Record("AfterCreate");
                        return ValueTask.CompletedTask;
                    })
                    .BeforeUpdate((id, entity, ct) =>
                    {
                        HookCollector.Record("BeforeUpdate");
                        return new ValueTask<HeadHookResult<TestInvoice>?>((HeadHookResult<TestInvoice>?)null); // Success; proceed
                    })
                    .AfterUpdate(async (id, entity, ct) =>
                    {
                        HookCollector.Record("AfterUpdate");
                        await Task.CompletedTask;
                    })
                    .BeforeDelete(async (id, ct) =>
                    {
                        HookCollector.Record("BeforeDelete");
                        await Task.CompletedTask;
                    })
                    .AfterDelete(async (entity, ct) =>
                    {
                        HookCollector.Record("AfterDelete");
                        await Task.CompletedTask;
                    })
                    .CustomAction("pay", (invoice, _) =>
                    {
                        HookCollector.Record("CustomActionPay");
                        invoice.Status = "paid";
                        invoice.PaidAt = DateTime.UtcNow;
                        return Task.CompletedTask;
                    })
                    .CustomAction("archive", (invoice, _) =>
                    {
                        HookCollector.Record("CustomActionArchive");
                        invoice.Status = "archived";
                        return Task.CompletedTask;
                    })
                    .Build();
            });
        });

        _testServer = new TestServer(builder);
        _httpClient = _testServer.CreateClient();

        using (var scope = _testServer.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }
    }

    public HttpClient CreateClient()
    {
        return _httpClient ?? throw new InvalidOperationException("Factory not initialized");
    }

    public async Task ClearInvoicesAsync()
    {
        var dbContext = GetDbContext();
        dbContext.Invoices.RemoveRange(dbContext.Invoices);
        await dbContext.SaveChangesAsync();
    }

    public async Task SeedInvoiceAsync(TestInvoice invoice)
    {
        var dbContext = GetDbContext();
        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync();
    }

    public async Task<TestInvoice?> GetInvoiceAsync(int id)
    {
        var dbContext = GetDbContext();
        return await dbContext.Invoices.FindAsync(id);
    }

    public ValueTask DisposeAsync()
    {
        _testServer?.Dispose();
        _httpClient?.Dispose();
        return ValueTask.CompletedTask;
    }
}

public sealed class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options)
    {
    }

    public DbSet<TestInvoice> Invoices => Set<TestInvoice>();
}
