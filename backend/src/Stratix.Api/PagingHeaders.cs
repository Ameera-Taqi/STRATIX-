namespace Stratix.Api;

/// <summary>Writes standard pagination metadata onto the HTTP response headers.</summary>
public static class PagingHeaders
{
    public static void Apply(HttpResponse response, int total, int page, int pageSize, int totalPages)
    {
        response.Headers["X-Total-Count"] = total.ToString();
        response.Headers["X-Page"] = page.ToString();
        response.Headers["X-Page-Size"] = pageSize.ToString();
        response.Headers["X-Total-Pages"] = totalPages.ToString();
    }
}
