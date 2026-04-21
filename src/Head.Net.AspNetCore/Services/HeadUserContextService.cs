using Microsoft.AspNetCore.Http;

namespace Head.Net.AspNetCore;

/// <summary>
/// Extracts and provides user context from HTTP requests.
/// Supports custom user ID providers with sensible defaults (UserId claim parsing).
/// Prepares for Phase 3+ features like role-based authorization.
/// </summary>
public sealed class HeadUserContextService
{
    private readonly Func<HttpContext, int> _userIdProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeadUserContextService"/> class.
    /// </summary>
    /// <param name="userIdProvider">Optional custom user ID extraction logic. If null, defaults to parsing "UserId" claim.</param>
    public HeadUserContextService(Func<HttpContext, int>? userIdProvider = null)
    {
        _userIdProvider = userIdProvider ?? DefaultUserIdProvider;
    }

    /// <summary>
    /// Extracts the current user ID from the HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The user ID, or 0 if not authenticated or extraction fails.</returns>
    public int GetUserId(HttpContext context)
    {
        return _userIdProvider(context);
    }

    /// <summary>
    /// Default user ID provider: parses the "UserId" claim from the authentication principal.
    /// Returns 0 if the claim is missing or invalid.
    /// </summary>
    private static int DefaultUserIdProvider(HttpContext ctx)
    {
        var userIdClaim = ctx.User?.FindFirst("UserId")?.Value ?? "0";
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}
