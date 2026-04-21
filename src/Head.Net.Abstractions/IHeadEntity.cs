namespace Head.Net.Abstractions;

/// <summary>
/// Represents an entity that can be exposed through the Head.Net CRUD surface.
/// </summary>
/// <typeparam name="TKey">The entity identifier type.</typeparam>
public interface IHeadEntity<TKey>
{
    /// <summary>
    /// Gets or sets the entity identifier.
    /// </summary>
    TKey Id { get; set; }
}
