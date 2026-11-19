namespace LinkedInContentManager.Contracts.Common;

/// <summary>Standard paginated envelope every list endpoint returns.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>Standard error envelope — never contains a stack trace or secret.</summary>
public sealed record ApiErrorResponse(string Message, string? TraceId = null, IReadOnlyDictionary<string, string[]>? ValidationErrors = null);
