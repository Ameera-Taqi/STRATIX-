using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stratix.Application.Common;

/// <summary>JSON snapshots for audit old/new values.</summary>
public static class AuditSnapshot
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string? Serialize(object? value) =>
        value is null ? null : JsonSerializer.Serialize(value, Options);
}
