using Head.Net.Abstractions;

namespace Head.Net.AspNetCore;

/// <summary>
/// Centralizes entity endpoint configuration in a dedicated class,
/// following the same pattern as EF Core's <c>IEntityTypeConfiguration&lt;T&gt;</c>.
/// </summary>
/// <typeparam name="TEntity">The entity type to configure.</typeparam>
public interface IHeadEntitySetup<TEntity>
    where TEntity : class, IHeadEntity<int>
{
    /// <summary>
    /// Configures the entity endpoint builder.
    /// Called by <see cref="HeadEntityEndpointBuilder{TEntity}.Setup{TSetup}"/> after
    /// any preceding builder calls have already been applied.
    /// </summary>
    /// <param name="builder">The builder to configure.</param>
    void Configure(HeadEntityEndpointBuilder<TEntity> builder);
}
