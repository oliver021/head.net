using Head.Net.Abstractions;
using Head.Net.AspNetCore;
using Head.Net.Tests.Fixtures;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Head.Net.Tests;

public sealed class HeadEntityEndpointIntegrationTests : IAsyncLifetime
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
    public async Task GetList_Returns_200_With_Invoices()
    {
        var invoice1 = new TestInvoice { CustomerName = "Acme", Total = 100m };
        var invoice2 = new TestInvoice { CustomerName = "Contoso", Total = 200m };
        await _factory.SeedInvoiceAsync(invoice1);
        await _factory.SeedInvoiceAsync(invoice2);

        var response = await _client.GetAsync("/invoices");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = JsonSerializer.Deserialize<HeadPagedResult<TestInvoice>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Data.Count);
    }

    [Fact]
    public async Task GetList_Empty_Returns_200_With_Empty_Data()
    {
        var response = await _client.GetAsync("/invoices");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = JsonSerializer.Deserialize<HeadPagedResult<TestInvoice>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task GetSingle_Returns_200_With_Invoice()
    {
        var invoice = new TestInvoice { CustomerName = "Acme", Total = 100m };
        await _factory.SeedInvoiceAsync(invoice);

        var response = await _client.GetAsync($"/invoices/{invoice.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = JsonSerializer.Deserialize<TestInvoice>(await response.Content.ReadAsStringAsync(), new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.Equal(invoice.Id, result.Id);
        Assert.Equal("Acme", result.CustomerName);
    }

    [Fact]
    public async Task GetSingle_Missing_Returns_404()
    {
        var response = await _client.GetAsync("/invoices/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostCreate_Returns_201_And_Persists()
    {
        var invoice = new { CustomerName = "NewCo", Total = 150m };

        var response = await _client.PostAsJsonAsync("/invoices", invoice);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = JsonSerializer.Deserialize<TestInvoice>(await response.Content.ReadAsStringAsync(), new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(created);
        Assert.NotEqual(0, created.Id);
        Assert.Equal("NewCo", created.CustomerName);
        Assert.Equal("draft", created.Status);

        var persisted = await _factory.GetInvoiceAsync(created.Id);
        Assert.NotNull(persisted);
        Assert.Equal("NewCo", persisted.CustomerName);
    }

    [Fact]
    public async Task PostCreate_Invokes_BeforeCreate_Hook()
    {
        _factory.HookCollector.Clear();
        var invoice = new { CustomerName = "NewCo", Total = 150m };

        await _client.PostAsJsonAsync("/invoices", invoice);

        Assert.True(_factory.HookCollector.WasHookCalled("BeforeCreate"));
    }

    [Fact]
    public async Task PostCreate_Invokes_AfterCreate_Hook()
    {
        _factory.HookCollector.Clear();
        var invoice = new { CustomerName = "NewCo", Total = 150m };

        await _client.PostAsJsonAsync("/invoices", invoice);

        Assert.True(_factory.HookCollector.WasHookCalled("AfterCreate"));
    }

    [Fact]
    public async Task PutUpdate_Returns_200_And_Persists()
    {
        var invoice = new TestInvoice { CustomerName = "OldCo", Total = 100m };
        await _factory.SeedInvoiceAsync(invoice);

        var updated = new { id = invoice.Id, CustomerName = "NewCo", Total = 200m, Status = "pending", CreatedAt = DateTime.UtcNow, PaidAt = (DateTime?)null, OwnerId = 0 };
        var response = await _client.PutAsJsonAsync($"/invoices/{invoice.Id}", updated);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = JsonSerializer.Deserialize<TestInvoice>(await response.Content.ReadAsStringAsync(), new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.Equal("NewCo", result.CustomerName);
        Assert.Equal(200m, result.Total);

        var persisted = await _factory.GetInvoiceAsync(invoice.Id);
        Assert.NotNull(persisted);
        Assert.Equal("NewCo", persisted.CustomerName);
    }

    [Fact]
    public async Task PutUpdate_Invokes_BeforeUpdate_Hook()
    {
        var invoice = new TestInvoice { CustomerName = "OldCo", Total = 100m };
        await _factory.SeedInvoiceAsync(invoice);
        _factory.HookCollector.Clear();

        var updated = new { id = invoice.Id, CustomerName = "NewCo", Total = 200m, Status = "pending", CreatedAt = DateTime.UtcNow, PaidAt = (DateTime?)null, OwnerId = 0 };
        await _client.PutAsJsonAsync($"/invoices/{invoice.Id}", updated);

        Assert.True(_factory.HookCollector.WasHookCalled("BeforeUpdate"));
    }

    [Fact]
    public async Task PutUpdate_Invokes_AfterUpdate_Hook()
    {
        var invoice = new TestInvoice { CustomerName = "OldCo", Total = 100m };
        await _factory.SeedInvoiceAsync(invoice);
        _factory.HookCollector.Clear();

        var updated = new { id = invoice.Id, CustomerName = "NewCo", Total = 200m, Status = "pending", CreatedAt = DateTime.UtcNow, PaidAt = (DateTime?)null, OwnerId = 0 };
        await _client.PutAsJsonAsync($"/invoices/{invoice.Id}", updated);

        Assert.True(_factory.HookCollector.WasHookCalled("AfterUpdate"));
    }

    [Fact]
    public async Task PutUpdate_Missing_Returns_404()
    {
        var updated = new { id = 999, CustomerName = "Ghost", Total = 100m, Status = "draft", CreatedAt = DateTime.UtcNow, PaidAt = (DateTime?)null, OwnerId = 0 };
        var response = await _client.PutAsJsonAsync("/invoices/999", updated);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteInvoice_Returns_200_And_Removes()
    {
        var invoice = new TestInvoice { CustomerName = "ToDelete", Total = 100m };
        await _factory.SeedInvoiceAsync(invoice);

        var response = await _client.DeleteAsync($"/invoices/{invoice.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var persisted = await _factory.GetInvoiceAsync(invoice.Id);
        Assert.Null(persisted);
    }

    [Fact]
    public async Task DeleteInvoice_Invokes_BeforeDelete_Hook()
    {
        var invoice = new TestInvoice { CustomerName = "ToDelete", Total = 100m };
        await _factory.SeedInvoiceAsync(invoice);
        _factory.HookCollector.Clear();

        await _client.DeleteAsync($"/invoices/{invoice.Id}");

        Assert.True(_factory.HookCollector.WasHookCalled("BeforeDelete"));
    }

    [Fact]
    public async Task DeleteInvoice_Invokes_AfterDelete_Hook()
    {
        var invoice = new TestInvoice { CustomerName = "ToDelete", Total = 100m };
        await _factory.SeedInvoiceAsync(invoice);
        _factory.HookCollector.Clear();

        await _client.DeleteAsync($"/invoices/{invoice.Id}");

        Assert.True(_factory.HookCollector.WasHookCalled("AfterDelete"));
    }

    [Fact]
    public async Task DeleteInvoice_Missing_Returns_404()
    {
        var response = await _client.DeleteAsync("/invoices/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_Respects_Skip_Parameter()
    {
        for (int i = 0; i < 5; i++)
        {
            await _factory.SeedInvoiceAsync(new TestInvoice { CustomerName = $"Customer{i}", Total = 100m });
        }

        var response = await _client.GetAsync("/invoices?skip=2&take=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = JsonSerializer.Deserialize<HeadPagedResult<TestInvoice>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    public async Task BeforeCreate_Sets_CreatedAt_Timestamp()
    {
        var before = DateTime.UtcNow;
        var invoice = new { CustomerName = "NewCo", Total = 150m };

        var response = await _client.PostAsJsonAsync("/invoices", invoice);
        var created = JsonSerializer.Deserialize<TestInvoice>(await response.Content.ReadAsStringAsync(), new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var after = DateTime.UtcNow;
        Assert.NotNull(created);
        Assert.True(created.CreatedAt >= before && created.CreatedAt <= after);
    }
}
