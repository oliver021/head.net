namespace Head.Net.Abstractions;

/// <summary>
/// Configures which CRUD operations are exposed for an entity surface.
/// </summary>
public sealed class HeadCrudOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether list endpoints are enabled.
    /// </summary>
    public bool EnableList { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether get-by-id endpoints are enabled.
    /// </summary>
    public bool EnableGet { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether create endpoints are enabled.
    /// </summary>
    public bool EnableCreate { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether update endpoints are enabled.
    /// </summary>
    public bool EnableUpdate { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether delete endpoints are enabled.
    /// </summary>
    public bool EnableDelete { get; set; } = true;
}
