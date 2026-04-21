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
    public static HeadEntityEndpointBuilder<TEntity> MapEntity<TEntity>(
        this IEndpointRouteBuilder endpoints,
        string? routePattern = null)
        where TEntity : class, IHeadEntity<int>
    {
        return new HeadEntityEndpointBuilder<TEntity>(endpoints, routePattern ?? GetDefaultRoute(typeof(TEntity).Name));
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
