namespace StoneBridge.Application.Common.Models;

/// <summary>
/// Wraps a page of query results with pagination metadata.
/// Used by all list query handlers.
/// Immutable — created via the static Create() factory method.
/// </summary>
public sealed class PagedResult<T>
{
    private PagedResult() { }

    public IReadOnlyList<T> Items           { get; private init; } = [];
    public int              TotalCount      { get; private init; }
    public int              Page            { get; private init; }
    public int              PerPage         { get; private init; }
    public bool             HasNextPage     => Page * PerPage < TotalCount;
    public bool             HasPreviousPage => Page > 1;
    public int              TotalPages      => PerPage > 0
                                                ? (int)Math.Ceiling((double)TotalCount / PerPage)
                                                : 0;

    public static PagedResult<T> Create(
        IEnumerable<T> items,
        int            totalCount,
        int            page,
        int            perPage)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(perPage, 1);

        return new PagedResult<T>
        {
            Items      = items.ToList().AsReadOnly(),
            TotalCount = totalCount,
            Page       = page,
            PerPage    = perPage,
        };
    }

    public static PagedResult<T> Empty(int page = 1, int perPage = 24)
        => Create([], 0, page, perPage);

    /// <summary>Project each item to a different type preserving pagination metadata.</summary>
    public PagedResult<TOut> Map<TOut>(Func<T, TOut> mapper)
        => PagedResult<TOut>.Create(Items.Select(mapper), TotalCount, Page, PerPage);
}