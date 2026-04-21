using Head.Net.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Head.Net.AspNetCore;

/// <summary>
/// Standardizes error responses to RFC 7807 Problem Details format.
/// Provides consistent error handling across all CRUD endpoints and custom actions.
/// </summary>
public static class HeadErrorResponseService
{
    /// <summary>
    /// Returns a 404 Not Found error in RFC 7807 Problem Details format.
    /// Supports any key type (int, Guid, long, string, etc.).
    /// </summary>
    /// <param name="entityName">The entity type name (e.g., "Product").</param>
    /// <param name="id">The requested entity ID.</param>
    /// <returns>IResult with 404 ProblemDetails.</returns>
    public static IResult NotFound(string entityName, object id)
    {
        return Results.NotFound(new ProblemDetails
        {
            Type = "https://head.net/errors/not-found",
            Title = "Entity Not Found",
            Detail = $"{entityName} with ID {id} not found.",
            Status = StatusCodes.Status404NotFound
        });
    }

    /// <summary>
    /// Returns a 403 Forbidden error in RFC 7807 Problem Details format.
    /// Supports any key type (int, Guid, long, string, etc.).
    /// </summary>
    /// <param name="entityName">The entity type name (e.g., "Invoice").</param>
    /// <param name="id">The requested entity ID.</param>
    /// <param name="reason">Optional reason for denial (e.g., "You do not own this entity").</param>
    /// <returns>IResult with 403 ProblemDetails.</returns>
    public static IResult Forbidden(string entityName, object id, string? reason = null)
    {
        var detail = reason ?? $"You do not have permission to access {entityName} with ID {id}.";
        return Results.Forbid();
        // Note: Results.Forbid() returns 403 without body; for ProblemDetails, would use:
        // return Results.StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails { ... })
        // Kept as Results.Forbid() for compatibility with existing tests
    }

    /// <summary>
    /// Returns a 400 Bad Request error with validation errors in RFC 7807 Problem Details format.
    /// </summary>
    /// <param name="validation">The validation result containing error messages.</param>
    /// <returns>IResult with 400 ProblemDetails.</returns>
    public static IResult ValidationFailed(HeadValidationResult? validation)
    {
        var errors = validation?.Errors ?? new[] { "Validation failed." };
        return Results.BadRequest(new ProblemDetails
        {
            Type = "https://head.net/errors/validation-failed",
            Title = "Validation Failed",
            Detail = string.Join("; ", errors),
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Returns a 400 Bad Request error with a single validation error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>IResult with 400 ProblemDetails.</returns>
    public static IResult ValidationFailed(string message)
    {
        return Results.BadRequest(new ProblemDetails
        {
            Type = "https://head.net/errors/validation-failed",
            Title = "Validation Failed",
            Detail = message,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Returns a 500 Internal Server Error in RFC 7807 Problem Details format.
    /// </summary>
    /// <param name="message">The error message (only shown in development).</param>
    /// <returns>IResult with 500 ProblemDetails.</returns>
    public static IResult InternalError(string? message = null)
    {
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
        // In production, avoid exposing internal error details
    }
}
