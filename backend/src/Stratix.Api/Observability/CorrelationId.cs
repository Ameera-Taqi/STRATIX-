using System.Diagnostics;

namespace Stratix.Api.Observability;

/// <summary>Correlation / request-id helpers. Never log Authorization or token values.</summary>
public static class CorrelationId
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "Stratix.CorrelationId";

    public static string GetOrCreate(HttpContext context)
    {
        if (context.Items.TryGetValue(ItemKey, out var existing) && existing is string s && !string.IsNullOrWhiteSpace(s))
            return s;

        var fromHeader = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(fromHeader))
            fromHeader = Activity.Current?.TraceId.ToString();

        var id = string.IsNullOrWhiteSpace(fromHeader)
            ? Guid.NewGuid().ToString("N")
            : fromHeader.Trim();

        if (id.Length > 128)
            id = id[..128];

        context.Items[ItemKey] = id;
        return id;
    }
}
