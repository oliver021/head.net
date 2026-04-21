using Head.Net.Abstractions;

namespace Head.Net.AspNetCore;

/// <summary>
/// Describes a named action that hangs off an entity route.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public sealed class HeadEntityActionDefinition<TEntity>
    where TEntity : class, IHeadEntity<int>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeadEntityActionDefinition{TEntity}"/> class.
    /// </summary>
    public HeadEntityActionDefinition(string name, string httpMethod, Func<TEntity, CancellationToken, Task> handler)
    {
        Name = name;
        HttpMethod = httpMethod;
        Handler = handler;
    }

    /// <summary>
    /// Gets the action name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the HTTP method used for the action route.
    /// </summary>
    public string HttpMethod { get; }

    /// <summary>
    /// Gets the action handler.
    /// </summary>
    public Func<TEntity, CancellationToken, Task> Handler { get; }
}
