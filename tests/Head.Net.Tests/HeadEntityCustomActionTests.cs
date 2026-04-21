using Head.Net.Tests.Fixtures;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Head.Net.Tests;

public sealed class HeadEntityCustomActionTests : IAsyncLifetime
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
    public async Task CustomAction_Pay_Changes_Status()
    {
        var invoice = new TestInvoice { CustomerName = "Test", Total = 100m, Status = "draft" };
        await _factory.SeedInvoiceAsync(invoice);

        var response = await _client.PostAsync($"/invoices/{invoice.Id}/pay", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var persisted = await _factory.GetInvoiceAsync(invoice.Id);
        Assert.NotNull(persisted);
        Assert.Equal("paid", persisted.Status);
    }

    [Fact]
    public async Task CustomAction_Pay_Sets_PaidAt_Timestamp()
    {
        var invoice = new TestInvoice { CustomerName = "Test", Total = 100m, Status = "draft" };
        await _factory.SeedInvoiceAsync(invoice);
        var before = DateTime.UtcNow;

        await _client.PostAsync($"/invoices/{invoice.Id}/pay", null);

        var after = DateTime.UtcNow;
        var persisted = await _factory.GetInvoiceAsync(invoice.Id);
        Assert.NotNull(persisted);
        Assert.NotNull(persisted.PaidAt);
        Assert.True(persisted.PaidAt >= before && persisted.PaidAt <= after);
    }

    [Fact]
    public async Task CustomAction_Archive_Changes_Status()
    {
        var invoice = new TestInvoice { CustomerName = "Test", Total = 100m, Status = "draft" };
        await _factory.SeedInvoiceAsync(invoice);

        var response = await _client.PostAsync($"/invoices/{invoice.Id}/archive", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var persisted = await _factory.GetInvoiceAsync(invoice.Id);
        Assert.NotNull(persisted);
        Assert.Equal("archived", persisted.Status);
    }

    [Fact]
    public async Task CustomAction_Missing_Entity_Returns_404()
    {
        var response = await _client.PostAsync("/invoices/999/pay", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CustomAction_Returns_Modified_Entity()
    {
        var invoice = new TestInvoice { CustomerName = "Test", Total = 100m, Status = "draft" };
        await _factory.SeedInvoiceAsync(invoice);

        var response = await _client.PostAsync($"/invoices/{invoice.Id}/pay", null);
        var result = JsonSerializer.Deserialize<TestInvoice>(await response.Content.ReadAsStringAsync(), new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal("paid", result.Status);
        Assert.NotNull(result.PaidAt);
    }

    [Fact]
    public async Task CustomAction_Pay_Executes_Hook()
    {
        var invoice = new TestInvoice { CustomerName = "Test", Total = 100m, Status = "draft" };
        await _factory.SeedInvoiceAsync(invoice);
        _factory.HookCollector.Clear();

        await _client.PostAsync($"/invoices/{invoice.Id}/pay", null);

        Assert.True(_factory.HookCollector.WasHookCalled("CustomActionPay"));
    }

    [Fact]
    public async Task CustomAction_Archive_Executes_Hook()
    {
        var invoice = new TestInvoice { CustomerName = "Test", Total = 100m, Status = "draft" };
        await _factory.SeedInvoiceAsync(invoice);
        _factory.HookCollector.Clear();

        await _client.PostAsync($"/invoices/{invoice.Id}/archive", null);

        Assert.True(_factory.HookCollector.WasHookCalled("CustomActionArchive"));
    }

    [Fact]
    public async Task CustomAction_Persists_Mutations()
    {
        var invoice = new TestInvoice { CustomerName = "Test", Total = 100m, Status = "draft" };
        await _factory.SeedInvoiceAsync(invoice);

        await _client.PostAsync($"/invoices/{invoice.Id}/pay", null);

        var persisted = await _factory.GetInvoiceAsync(invoice.Id);
        Assert.NotNull(persisted);
        Assert.Equal("paid", persisted.Status);
        Assert.NotNull(persisted.PaidAt);
    }

    [Fact]
    public async Task CustomAction_Pay_Idempotent_On_Already_Paid()
    {
        var invoice = new TestInvoice { CustomerName = "Test", Total = 100m, Status = "paid", PaidAt = DateTime.UtcNow };
        await _factory.SeedInvoiceAsync(invoice);
        var originalPaidAt = invoice.PaidAt;

        var response = await _client.PostAsync($"/invoices/{invoice.Id}/pay", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var persisted = await _factory.GetInvoiceAsync(invoice.Id);
        Assert.NotNull(persisted);
        Assert.Equal("paid", persisted.Status);
    }

    [Fact]
    public async Task Multiple_CustomActions_Can_Modify_Same_Entity()
    {
        var invoice = new TestInvoice { CustomerName = "Test", Total = 100m, Status = "draft" };
        await _factory.SeedInvoiceAsync(invoice);

        await _client.PostAsync($"/invoices/{invoice.Id}/pay", null);
        var persisted1 = await _factory.GetInvoiceAsync(invoice.Id);
        Assert.NotNull(persisted1);
        Assert.Equal("paid", persisted1.Status);

        await _client.PostAsync($"/invoices/{invoice.Id}/archive", null);
        var persisted2 = await _factory.GetInvoiceAsync(invoice.Id);
        Assert.NotNull(persisted2);
        Assert.Equal("archived", persisted2.Status);
    }
}
