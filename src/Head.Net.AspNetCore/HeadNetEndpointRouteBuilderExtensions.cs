using Head.Net.Abstractions;
using Microsoft.AspNetCore.Routing;

namespace Head.Net.AspNetCore;

/// <summary>
/// Adds Head.Net entity mapping extensions to minimal API route builders.
/// </summary>
public static class HeadNetEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Starts building a conventional entity route surface.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TKey">The entity's primary key type.</typeparam>
    public static HeadEntityEndpointBuilder<TEntity, TKey> MapEntity<TEntity, TKey>(
        this IEndpointRouteBuilder endpoints,
        string? routePattern = null)
        where TEntity : class, IHeadEntity<TKey>
        where TKey : notnull, IEquatable<TKey>
    {
        return new HeadEntityEndpointBuilder<TEntity, TKey>(endpoints, routePattern ?? GetDefaultRoute(typeof(TEntity).Name));
    }

    /// <summary>
    /// Starts building a conventional entity route surface with int primary key.
    /// Convenience overload for entities with int primary keys.
    /// </summary>
    public static HeadEntityEndpointBuilder<TEntity, int> MapEntity<TEntity>(
        this IEndpointRouteBuilder endpoints,
        string? routePattern = null)
        where TEntity : class, IHeadEntity<int>
    {
        return endpoints.MapEntity<TEntity, int>(routePattern);
    }

    private static string GetDefaultRoute(string entityName)
    {
        if (entityName.EndsWith("y", StringComparison.OrdinalIgnoreCase))
        {
            return "/" + entityName[..^1].ToLowerInvariant() + "ies";
        }

        return "/" + entityName.ToLowerInvariant() + "s";
    }
}
