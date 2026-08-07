using Stratix.Application.Common;

namespace Stratix.Api;

/// <summary>Writes standard pagination metadata onto the HTTP response headers.</summary>
public static class PagingHeaders
{
    public static readonly string[] ExposedNames =
    [
        "X-Total-Count",
        "X-Page",
        "X-Page-Size",
        "X-Total-Pages"
    ];

    public static void Apply(HttpResponse response, int total, int page, int pageSize, int totalPages)
    {
        response.Headers["X-Total-Count"] = total.ToString();
        response.Headers["X-Page"] = page.ToString();
        response.Headers["X-Page-Size"] = pageSize.ToString();
        response.Headers["X-Total-Pages"] = totalPages.ToString();
    }

    public static void Apply<T>(HttpResponse response, PagedResult<T> page) =>
        Apply(response, page.Total, page.Page, page.PageSize, page.TotalPages);

    public static void Apply<T>(HttpResponse response, PagedResponse<T> page) =>
        Apply(response, page.Total, page.Page, page.PageSize, page.TotalPages);
}
