using Head.Net.Abstractions;

namespace Head.Net.Tests.Fixtures;

public sealed class TestInvoice : IHeadEntity<int>
{
    public int Id { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public decimal Total { get; set; }

    public string Status { get; set; } = "draft";

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public int OwnerId { get; set; }
}
