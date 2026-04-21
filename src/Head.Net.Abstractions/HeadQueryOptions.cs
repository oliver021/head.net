namespace Head.Net.Abstractions;

/// <summary>
/// Captures filtering, sorting, and pagination options for entity queries.
/// </summary>
public sealed class HeadQueryOptions
{
    /// <summary>
    /// Gets or sets the number of items to skip.
    /// </summary>
    public int Skip { get; set; } = 0;

    /// <summary>
    /// Gets or sets the maximum number of items to return.
    /// </summary>
    public int Take { get; set; } = 100;

    /// <summary>
    /// Gets or sets the comma-separated list of properties to sort by. Use '-' prefix for descending.
    /// Example: "Total,-CreatedAt"
    /// </summary>
    public string? OrderBy { get; set; }

    /// <summary>
    /// Gets or sets the filters to apply. Key-value pairs for simple equality matching.
    /// </summary>
    public Dictionary<string, string> Filters { get; } = [];
}

/// <summary>
/// Represents a paginated query result.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public sealed class HeadPagedResult<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeadPagedResult{TEntity}"/> class.
    /// </summary>
    public HeadPagedResult(IReadOnlyList<TEntity> data, int totalCount, int skip, int take)
    {
        Data = data;
        TotalCount = totalCount;
        Skip = skip;
        Take = take;
    }

    /// <summary>
    /// Gets the items in this page.
    /// </summary>
    public IReadOnlyList<TEntity> Data { get; }

    /// <summary>
    /// Gets the total number of items matching the query.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Gets the number of items skipped.
    /// </summary>
    public int Skip { get; }

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int Take { get; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int PageCount => (TotalCount + Take - 1) / Take;

    /// <summary>
    /// Gets the current page number (0-indexed).
    /// </summary>
    public int PageNumber => Skip / Take;
}

/// <summary>
/// Represents a validation error result that halts the CRUD operation.
/// </summary>
public sealed class HeadValidationResult
{
    private readonly List<string> errors = [];

    /// <summary>
    /// Gets a value indicating whether the result is valid (no errors).
    /// </summary>
    public bool IsValid => errors.Count == 0;

    /// <summary>
    /// Gets the error messages.
    /// </summary>
    public IReadOnlyList<string> Errors => errors.AsReadOnly();

    /// <summary>
    /// Adds an error message.
    /// </summary>
    public void AddError(string message) => errors.Add(message);

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static HeadValidationResult Success() => new();

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static HeadValidationResult Failure(params string[] errors)
    {
        var result = new HeadValidationResult();
        foreach (var error in errors)
        {
            result.AddError(error);
        }
        return result;
    }
}

/// <summary>
/// Represents the result of a hook operation that can short-circuit the CRUD pipeline.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public sealed class HeadHookResult<TEntity>
    where TEntity : class
{
    private HeadHookResult() { }

    /// <summary>
    /// Gets a value indicating whether the operation should proceed.
    /// </summary>
    public bool ShouldProceed { get; private init; }

    /// <summary>
    /// Gets the validation errors that caused the short-circuit.
    /// </summary>
    public HeadValidationResult? ValidationResult { get; private init; }

    /// <summary>
    /// Creates a result indicating the operation should proceed.
    /// </summary>
    public static HeadHookResult<TEntity> Continue() => new() { ShouldProceed = true };

    /// <summary>
    /// Creates a result with validation errors.
    /// </summary>
    public static HeadHookResult<TEntity> Invalid(HeadValidationResult validation) =>
        new() { ShouldProceed = false, ValidationResult = validation };
}
