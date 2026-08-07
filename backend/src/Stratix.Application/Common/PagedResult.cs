namespace Stratix.Application.Common;

/// <summary>A single page of results plus the total row count for the query.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);

    public PagedResponse<T> ToResponse() => new(Items, Total, Page, PageSize, TotalPages);
}

/// <summary>Standard JSON envelope for paged API responses (1-based page).</summary>
public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Total,
    int Page,
    int PageSize,
    int TotalPages);

/// <summary>Normalizes raw page/pageSize query input into safe bounds (1-based).</summary>
public static class PageQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>
    /// Clamps page to ≥ 1 and pageSize to [1, <see cref="MaxPageSize"/>].
    /// Oversized values such as <c>?pageSize=1000000</c> become <see cref="MaxPageSize"/>.
    /// </summary>
    public static (int page, int pageSize) Normalize(int? page, int? pageSize, int defaultPageSize = DefaultPageSize)
    {
        var p = page is > 0 ? page.Value : 1;
        var fallback = Math.Clamp(defaultPageSize, 1, MaxPageSize);
        var size = pageSize is > 0
            ? Math.Clamp(pageSize.Value, 1, MaxPageSize)
            : fallback;
        return (p, size);
    }
}
