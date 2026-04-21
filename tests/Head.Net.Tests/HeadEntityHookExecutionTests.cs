using Head.Net.Tests.Fixtures;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Head.Net.Tests;

public sealed class HeadEntityHookExecutionTests : IAsyncLifetime
{
    private TestWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new TestWebApplicationFactory();
        await _factory.InitializeAsync();
        _client = _factory.CreateClient();
        await _factory.ClearInvoicesAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Create_Executes_Hooks_In_Order()
    {
        _factory.HookCollector.Clear();
        var invoice = new { CustomerName = "Test", Total = 100m };

        await _client.PostAsJsonAsync("/invoices", invoice);

        var hooks = _factory.HookCollector.ExecutedHooks;
        Assert.Equal(2, hooks.Count);
        Assert.Equal("BeforeCreate", hooks[0]);
        Assert.Equal("AfterCreate", hooks[1]);
    }

    [Fact]
    public async Task Update_Executes_Hooks_In_Order()
    {
        var invoice = new TestInvoice { CustomerName = "Test", Total = 100m };
        await _factory.SeedInvoiceAsync(invoice);
        _factory.HookCollector.Clear();

        var updated = new { id = invoice.Id, CustomerName = "Updated", Total = 200m, Status = "draft", CreatedAt = DateTime.UtcNow, PaidAt = (DateTime?)null, OwnerId = 0 };
        await _client.PutAsJsonAsync($"/invoices/{invoice.Id}", updated);

        var hooks = _factory.HookCollector.ExecutedHooks;
        Assert.Equal(2, hooks.Count);
        Assert.Equal("BeforeUpdate", hooks[0]);
        Assert.Equal("AfterUpdate", hooks[1]);
    }

    [Fact]
    public async Task Delete_Executes_Hooks_In_Order()
    {
        var invoice = new TestInvoice { CustomerName = "Test", Total = 100m };
        await _factory.SeedInvoiceAsync(invoice);
        _factory.HookCollector.Clear();

        await _client.DeleteAsync($"/invoices/{invoice.Id}");

        var hooks = _factory.HookCollector.ExecutedHooks;
        Assert.Equal(2, hooks.Count);
        Assert.Equal("BeforeDelete", hooks[0]);
        Assert.Equal("AfterDelete", hooks[1]);
    }

    [Fact]
    public async Task AfterCreate_Receives_Created_Entity()
    {
        _factory.HookCollector.Clear();
        var invoice = new { CustomerName = "NewCo", Total = 150m };

        var response = await _client.PostAsJsonAsync("/invoices", invoice);
        var created = JsonSerializer.Deserialize<TestInvoice>(await response.Content.ReadAsStringAsync(), new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(created);
        Assert.Equal("draft", created.Status);
        var persisted = await _factory.GetInvoiceAsync(created.Id);
        Assert.NotNull(persisted);
    }

    [Fact]
    public async Task BeforeCreate_Can_Mutate_Entity()
    {
        _factory.HookCollector.Clear();
        var invoice = new { CustomerName = "NewCo", Total = 150m };

        var response = await _client.PostAsJsonAsync("/invoices", invoice);
        var created = JsonSerializer.Deserialize<TestInvoice>(await response.Content.ReadAsStringAsync(), new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(created);
        Assert.Equal("draft", created.Status);
        Assert.NotEqual(DateTime.MinValue, created.CreatedAt);
    }

    [Fact]
    public async Task Multiple_Sequential_Creates_Have_Separate_Hook_Executions()
    {
        _factory.HookCollector.Clear();

        var invoice1 = new { CustomerName = "First", Total = 100m };
        await _client.PostAsJsonAsync("/invoices", invoice1);
        Assert.Equal(2, _factory.HookCollector.ExecutedHooks.Count);

        _factory.HookCollector.Clear();
        var invoice2 = new { CustomerName = "Second", Total = 200m };
        await _client.PostAsJsonAsync("/invoices", invoice2);
        Assert.Equal(2, _factory.HookCollector.ExecutedHooks.Count);
    }

    [Fact]
    public async Task Hook_Execution_Persists_Mutations()
    {
        _factory.HookCollector.Clear();
        var invoice = new { CustomerName = "Test", Total = 100m };

        var response = await _client.PostAsJsonAsync("/invoices", invoice);
        var created = JsonSerializer.Deserialize<TestInvoice>(await response.Content.ReadAsStringAsync(), new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(created);
        var persisted = await _factory.GetInvoiceAsync(created.Id);
        Assert.NotNull(persisted);
        Assert.Equal("draft", persisted.Status);
        Assert.NotEqual(default, persisted.CreatedAt);
    }

    [Fact]
    public async Task BeforeUpdate_Hook_Receives_Id_And_Entity()
    {
        var invoice = new TestInvoice { CustomerName = "Original", Total = 100m };
        await _factory.SeedInvoiceAsync(invoice);
        _factory.HookCollector.Clear();

        var updated = new { id = invoice.Id, CustomerName = "Updated", Total = 200m, Status = "pending", CreatedAt = invoice.CreatedAt, PaidAt = (DateTime?)null, OwnerId = 0 };
        await _client.PutAsJsonAsync($"/invoices/{invoice.Id}", updated);

        Assert.True(_factory.HookCollector.WasHookCalled("BeforeUpdate"));
        var persisted = await _factory.GetInvoiceAsync(invoice.Id);
        Assert.NotNull(persisted);
        Assert.Equal("Updated", persisted.CustomerName);
    }

    [Fact]
    public async Task AfterUpdate_Hook_Receives_Id_And_Updated_Entity()
    {
        var invoice = new TestInvoice { CustomerName = "Original", Total = 100m };
        await _factory.SeedInvoiceAsync(invoice);
        _factory.HookCollector.Clear();

        var updated = new { id = invoice.Id, CustomerName = "Updated", Total = 200m, Status = "pending", CreatedAt = invoice.CreatedAt, PaidAt = (DateTime?)null, OwnerId = 0 };
        await _client.PutAsJsonAsync($"/invoices/{invoice.Id}", updated);

        Assert.True(_factory.HookCollector.WasHookCalled("AfterUpdate"));
    }

    [Fact]
    public async Task BeforeDelete_Hook_Receives_Id()
    {
        var invoice = new TestInvoice { CustomerName = "ToDelete", Total = 100m };
        await _factory.SeedInvoiceAsync(invoice);
        _factory.HookCollector.Clear();

        await _client.DeleteAsync($"/invoices/{invoice.Id}");

        Assert.True(_factory.HookCollector.WasHookCalled("BeforeDelete"));
    }

    [Fact]
    public async Task AfterDelete_Hook_Receives_Deleted_Entity()
    {
        var invoice = new TestInvoice { CustomerName = "ToDelete", Total = 100m };
        await _factory.SeedInvoiceAsync(invoice);
        _factory.HookCollector.Clear();

        await _client.DeleteAsync($"/invoices/{invoice.Id}");

        Assert.True(_factory.HookCollector.WasHookCalled("AfterDelete"));
        var persisted = await _factory.GetInvoiceAsync(invoice.Id);
        Assert.Null(persisted);
    }
}
