namespace Stratix.Application.Common;

/// <summary>A single page of results plus the total row count for the query.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}

/// <summary>Normalizes raw page/pageSize query input into safe bounds.</summary>
public static class PageQuery
{
    public const int MaxPageSize = 100;

    public static (int page, int pageSize) Normalize(int? page, int? pageSize)
    {
        var p = page is > 0 ? page.Value : 1;
        var size = pageSize is > 0 ? Math.Min(pageSize.Value, MaxPageSize) : 20;
        return (p, size);
    }
}
