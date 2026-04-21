using Head.Net.Tests.Fixtures;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Head.Net.Tests;

public sealed class HeadEntityErrorScenariosTests : IAsyncLifetime
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
    public async Task GetNonExistent_Returns_404()
    {
        var response = await _client.GetAsync("/invoices/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateNonExistent_Returns_404()
    {
        var updated = new { id = 999, CustomerName = "Ghost", Total = 100m, Status = "draft", CreatedAt = DateTime.UtcNow, PaidAt = (DateTime?)null, OwnerId = 0 };
        var response = await _client.PutAsJsonAsync("/invoices/999", updated);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteNonExistent_Returns_404()
    {
        var response = await _client.DeleteAsync("/invoices/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CustomActionNonExistent_Returns_404()
    {
        var response = await _client.PostAsync("/invoices/999/pay", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_With_Invalid_Body_Returns_BadRequest()
    {
        var response = await _client.PostAsync("/invoices", new StringContent("invalid json", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_With_Invalid_Body_Returns_BadRequest()
    {
        var response = await _client.PutAsync("/invoices/1", new StringContent("invalid json", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetList_InvalidSkip_Returns_BadRequest()
    {
        var response = await _client.GetAsync("/invoices?skip=invalid&take=10");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetList_InvalidTake_Returns_BadRequest()
    {
        var response = await _client.GetAsync("/invoices?skip=0&take=invalid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NegativeSkip_Returns_BadRequest()
    {
        var response = await _client.GetAsync("/invoices?skip=-1&take=10");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NegativeTake_Returns_BadRequest()
    {
        var response = await _client.GetAsync("/invoices?skip=0&take=-1");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RouteToNonExistentAction_Returns_404()
    {
        var invoice = new TestInvoice { CustomerName = "Test", Total = 100m };
        await _factory.SeedInvoiceAsync(invoice);

        var response = await _client.PostAsync($"/invoices/{invoice.Id}/nonexistent", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MultipleSequentialErrors_Do_Not_Affect_Success()
    {
        await _client.GetAsync("/invoices/999");
        await _client.DeleteAsync("/invoices/888");
        await _client.PostAsync("/invoices/777/pay", null);

        var invoice = new { CustomerName = "Test", Total = 100m };
        var response = await _client.PostAsJsonAsync("/invoices", invoice);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetAfter404_Works_Correctly()
    {
        await _client.GetAsync("/invoices/999");

        var invoice = new TestInvoice { CustomerName = "Test", Total = 100m };
        await _factory.SeedInvoiceAsync(invoice);

        var response = await _client.GetAsync($"/invoices/{invoice.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = JsonSerializer.Deserialize<TestInvoice>(await response.Content.ReadAsStringAsync(), new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.Equal("Test", result.CustomerName);
    }

    [Fact]
    public async Task CreateAfterDelete_Creates_New_Entity_With_Different_Id()
    {
        var invoice1 = new TestInvoice { CustomerName = "First", Total = 100m };
        await _factory.SeedInvoiceAsync(invoice1);
        var id1 = invoice1.Id;

        await _client.DeleteAsync($"/invoices/{id1}");

        var invoice2 = new { CustomerName = "Second", Total = 200m };
        var response = await _client.PostAsJsonAsync("/invoices", invoice2);
        var created = JsonSerializer.Deserialize<TestInvoice>(await response.Content.ReadAsStringAsync(), new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(created);
        Assert.NotEqual(id1, created.Id);
    }
}
