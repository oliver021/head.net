using Head.Net.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Head.Net.EntityFrameworkCore;

/// <summary>
/// Registers EF Core-backed Head.Net stores.
/// Supports any key type (int, Guid, long, string, etc.).
/// </summary>
public static class HeadNetEntityFrameworkServiceCollectionExtensions
{
    /// <summary>
    /// Registers a scoped <see cref="IHeadEntityStore{TEntity, TKey}"/> backed by the specified EF Core context.
    /// </summary>
    /// <typeparam name="TContext">The EF Core context type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TKey">The entity's primary key type.</typeparam>
    public static IServiceCollection AddHeadEntityStore<TContext, TEntity, TKey>(this IServiceCollection services)
        where TContext : DbContext
        where TEntity : class, IHeadEntity<TKey>
        where TKey : notnull, IEquatable<TKey>, IComparable<TKey>
    {
        services.AddScoped<IHeadEntityStore<TEntity, TKey>, HeadEntityDbContextStore<TContext, TEntity, TKey>>();
        return services;
    }

    /// <summary>
    /// Registers a scoped <see cref="IHeadEntityStore{TEntity, TKey}"/> backed by the specified EF Core context.
    /// Convenience method for entities with int primary keys.
    /// </summary>
    public static IServiceCollection AddHeadEntityStore<TContext, TEntity>(this IServiceCollection services)
        where TContext : DbContext
        where TEntity : class, IHeadEntity<int>
    {
        return services.AddHeadEntityStore<TContext, TEntity, int>();
    }
}
