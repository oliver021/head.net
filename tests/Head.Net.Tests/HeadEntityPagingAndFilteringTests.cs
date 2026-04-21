using Head.Net.Abstractions;
using Head.Net.AspNetCore;
using Head.Net.Tests.Fixtures;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Head.Net.Tests;

public sealed class HeadEntityPagingAndFilteringTests : IAsyncLifetime
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
    public async Task List_Default_Paging_Returns_First_Page()
    {
        for (int i = 0; i < 5; i++)
        {
            await _factory.SeedInvoiceAsync(new TestInvoice { CustomerName = $"Customer{i}", Total = 100m * (i + 1) });
        }

        var response = await _client.GetAsync("/invoices");
        var result = JsonSerializer.Deserialize<HeadPagedResult<TestInvoice>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(5, result.Data.Count);
        Assert.Equal(0, result.Skip);
        Assert.Equal(100, result.Take);
    }

    [Fact]
    public async Task List_Skip_Parameter_Skips_Entities()
    {
        for (int i = 0; i < 5; i++)
        {
            await _factory.SeedInvoiceAsync(new TestInvoice { CustomerName = $"Customer{i}", Total = 100m });
        }

        var response = await _client.GetAsync("/invoices?skip=2&take=2");
        var result = JsonSerializer.Deserialize<HeadPagedResult<TestInvoice>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(2, result.Skip);
        Assert.Equal(2, result.Take);
    }

    [Fact]
    public async Task List_Take_Parameter_Limits_Results()
    {
        for (int i = 0; i < 10; i++)
        {
            await _factory.SeedInvoiceAsync(new TestInvoice { CustomerName = $"Customer{i}", Total = 100m });
        }

        var response = await _client.GetAsync("/invoices?skip=0&take=3");
        var result = JsonSerializer.Deserialize<HeadPagedResult<TestInvoice>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal(10, result.TotalCount);
        Assert.Equal(3, result.Data.Count);
    }

    [Fact]
    public async Task List_PageCount_Calculated_Correctly()
    {
        for (int i = 0; i < 25; i++)
        {
            await _factory.SeedInvoiceAsync(new TestInvoice { CustomerName = $"Customer{i}", Total = 100m });
        }

        var response = await _client.GetAsync("/invoices?skip=0&take=10");
        var result = JsonSerializer.Deserialize<HeadPagedResult<TestInvoice>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.PageCount);
    }

    [Fact]
    public async Task List_Last_Page_Partial_Results()
    {
        for (int i = 0; i < 25; i++)
        {
            await _factory.SeedInvoiceAsync(new TestInvoice { CustomerName = $"Customer{i}", Total = 100m });
        }

        var response = await _client.GetAsync("/invoices?skip=20&take=10");
        var result = JsonSerializer.Deserialize<HeadPagedResult<TestInvoice>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(5, result.Data.Count);
    }

    [Fact]
    public async Task List_Empty_Results_Returns_Zero_Total()
    {
        var response = await _client.GetAsync("/invoices");
        var result = JsonSerializer.Deserialize<HeadPagedResult<TestInvoice>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task List_Single_Page_Results()
    {
        for (int i = 0; i < 5; i++)
        {
            await _factory.SeedInvoiceAsync(new TestInvoice { CustomerName = $"Customer{i}", Total = 100m });
        }

        var response = await _client.GetAsync("/invoices?skip=0&take=10");
        var result = JsonSerializer.Deserialize<HeadPagedResult<TestInvoice>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(5, result.Data.Count);
        Assert.Equal(1, result.PageCount);
    }

    [Fact]
    public async Task List_Skip_Beyond_Total_Returns_Empty()
    {
        for (int i = 0; i < 5; i++)
        {
            await _factory.SeedInvoiceAsync(new TestInvoice { CustomerName = $"Customer{i}", Total = 100m });
        }

        var response = await _client.GetAsync("/invoices?skip=100&take=10");
        var result = JsonSerializer.Deserialize<HeadPagedResult<TestInvoice>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal(5, result.TotalCount);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task List_Boundary_Skip_Zero()
    {
        for (int i = 0; i < 5; i++)
        {
            await _factory.SeedInvoiceAsync(new TestInvoice { CustomerName = $"Customer{i}", Total = 100m });
        }

        var response = await _client.GetAsync("/invoices?skip=0&take=5");
        var result = JsonSerializer.Deserialize<HeadPagedResult<TestInvoice>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal(5, result.Data.Count);
    }

    [Fact]
    public async Task List_Boundary_Take_One()
    {
        for (int i = 0; i < 5; i++)
        {
            await _factory.SeedInvoiceAsync(new TestInvoice { CustomerName = $"Customer{i}", Total = 100m });
        }

        var response = await _client.GetAsync("/invoices?skip=0&take=1");
        var result = JsonSerializer.Deserialize<HeadPagedResult<TestInvoice>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Single(result.Data);
    }

    [Fact]
    public async Task List_TotalCount_Reflects_All_Records()
    {
        var invoices = Enumerable.Range(1, 50)
            .Select(i => new TestInvoice { CustomerName = $"Customer{i}", Total = 100m })
            .ToList();

        foreach (var inv in invoices)
        {
            await _factory.SeedInvoiceAsync(inv);
        }

        var response = await _client.GetAsync("/invoices?skip=0&take=10");
        var result = JsonSerializer.Deserialize<HeadPagedResult<TestInvoice>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal(50, result.TotalCount);
        Assert.Equal(5, result.PageCount);
    }
}
