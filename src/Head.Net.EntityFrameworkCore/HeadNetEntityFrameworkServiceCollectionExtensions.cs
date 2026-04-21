using Head.Net.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Head.Net.EntityFrameworkCore;

/// <summary>
/// Registers EF Core-backed Head.Net stores.
/// </summary>
public static class HeadNetEntityFrameworkServiceCollectionExtensions
{
    /// <summary>
    /// Registers a scoped <see cref="IHeadEntityStore{TEntity}"/> backed by the specified EF Core context.
    /// </summary>
    public static IServiceCollection AddHeadEntityStore<TContext, TEntity>(this IServiceCollection services)
        where TContext : DbContext
        where TEntity : class, IHeadEntity<int>
    {
        services.AddScoped<IHeadEntityStore<TEntity>, HeadEntityDbContextStore<TContext, TEntity>>();
        return services;
    }
}
