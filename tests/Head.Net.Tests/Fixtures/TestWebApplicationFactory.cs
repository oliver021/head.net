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
    private TestDbContext? _dbContext;
    private HttpClient? _httpClient;
    private IServiceScope? _scope;

    public TestHookCollector HookCollector { get; } = new();

    public TestAuthorizationProvider AuthorizationProvider { get; } = new();

    public TestDbContext DbContext =>
        _dbContext ?? throw new InvalidOperationException("DbContext not initialized");

    public async Task InitializeAsync()
    {
        var builder = new WebHostBuilder();

        builder.ConfigureServices(services =>
        {
            services.AddRouting();

            services.AddDbContext<TestDbContext>(options =>
                options.UseInMemoryDatabase(_dbContextId)
            );

            services.AddScoped<IHeadEntityStore<TestInvoice>>(sp =>
                new HeadEntityDbContextStore<TestDbContext, TestInvoice>(
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
                    .BeforeCreate(async (entity, ct) =>
                    {
                        HookCollector.Record("BeforeCreate");
                        entity.CreatedAt = DateTime.UtcNow;
                        if (string.IsNullOrEmpty(entity.Status))
                            entity.Status = "draft";
                        await Task.CompletedTask;
                    })
                    .AfterCreate(async (entity, ct) =>
                    {
                        HookCollector.Record("AfterCreate");
                        await Task.CompletedTask;
                    })
                    .BeforeUpdate(async (id, entity, ct) =>
                    {
                        HookCollector.Record("BeforeUpdate");
                        await Task.CompletedTask;
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

        _scope = _testServer.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public HttpClient CreateClient()
    {
        return _httpClient ?? throw new InvalidOperationException("Factory not initialized");
    }

    public async Task ClearInvoicesAsync()
    {
        DbContext.Invoices.RemoveRange(DbContext.Invoices);
        await DbContext.SaveChangesAsync();
    }

    public async Task SeedInvoiceAsync(TestInvoice invoice)
    {
        DbContext.Invoices.Add(invoice);
        await DbContext.SaveChangesAsync();
    }

    public async Task<TestInvoice?> GetInvoiceAsync(int id)
    {
        return await DbContext.Invoices.FindAsync(id);
    }

    public ValueTask DisposeAsync()
    {
        _scope?.Dispose();
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
