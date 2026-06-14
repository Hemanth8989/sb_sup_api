namespace StoneBridge.Application.Common.Models;

/// <summary>
/// Standard JSON envelope returned by every API endpoint.
/// Every response — success or failure — has the same outer shape.
/// Clients always check the 'success' field first.
/// </summary>
public sealed class ApiResponse<T>
{
    private ApiResponse() { }

    public T?       Data      { get; private init; }
    public ApiMeta? Meta      { get; private init; }
    public bool     Success   => Error is null;
    public string?  Error     { get; private init; }
    public string?  ErrorCode { get; private init; }

    /// <summary>Create a successful response with data and optional pagination metadata.</summary>
    public static ApiResponse<T> Ok(T data, ApiMeta? meta = null) =>
        new() { Data = data, Meta = meta };

    /// <summary>Create a failure response with an error message and optional error code.</summary>
    public static ApiResponse<T> Fail(string error, string? errorCode = null) =>
        new() { Error = error, ErrorCode = errorCode };
}

/// <summary>
/// Pagination metadata included in list responses.
/// Allows clients to render pagination controls without knowing total count upfront.
/// </summary>
public sealed record ApiMeta(
    int  TotalCount,
    int  Page,
    int  PerPage,
    bool HasNextPage,
    bool HasPreviousPage,
    int  TotalPages)
{
    /// <summary>Build ApiMeta from a PagedResult instance.</summary>
    public static ApiMeta From<T>(PagedResult<T> paged) => new(
        paged.TotalCount,
        paged.Page,
        paged.PerPage,
        paged.HasNextPage,
        paged.HasPreviousPage,
        paged.TotalPages);
}