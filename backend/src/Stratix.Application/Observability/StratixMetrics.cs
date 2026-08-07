using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Stratix.Application.Observability;

/// <summary>
/// In-process counters + <see cref="System.Diagnostics.Metrics"/> instruments.
/// Values are aggregates only — never store passwords, tokens, or file contents.
/// </summary>
public sealed class StratixMetrics : IStratixMetrics
{
    public const string MeterName = "Stratix.Api";

    private static readonly Meter Meter = new(MeterName, "1.0.0");
    private static readonly Counter<long> FailedLogins = Meter.CreateCounter<long>(
        "stratix_auth_failed_logins_total",
        description: "Failed authentication attempts");
    private static readonly Counter<long> SubscriptionRejections = Meter.CreateCounter<long>(
        "stratix_subscription_rejections_total",
        description: "Blocked access due to subscription/org status");
    private static readonly Counter<long> AiFailures = Meter.CreateCounter<long>(
        "stratix_ai_analysis_failures_total",
        description: "AI project-health analysis failures");
    private static readonly Counter<long> StorageFailures = Meter.CreateCounter<long>(
        "stratix_storage_failures_total",
        description: "Tenant file storage I/O failures");

    private readonly ConcurrentDictionary<string, long> _snapshot = new(StringComparer.Ordinal);

    public void RecordFailedLogin(string reason)
    {
        var key = Normalize("auth.failed_login", reason);
        FailedLogins.Add(1, new KeyValuePair<string, object?>("reason", Sanitize(reason)));
        Increment(key);
    }

    public void RecordSubscriptionRejection(string reason)
    {
        var key = Normalize("subscription.rejection", reason);
        SubscriptionRejections.Add(1, new KeyValuePair<string, object?>("reason", Sanitize(reason)));
        Increment(key);
    }

    public void RecordAiAnalysisFailure(string reason)
    {
        var key = Normalize("ai.analysis_failure", reason);
        AiFailures.Add(1, new KeyValuePair<string, object?>("reason", Sanitize(reason)));
        Increment(key);
    }

    public void RecordStorageFailure(string operation, string reason)
    {
        var key = Normalize($"storage.{Sanitize(operation)}", reason);
        StorageFailures.Add(1,
            new KeyValuePair<string, object?>("operation", Sanitize(operation)),
            new KeyValuePair<string, object?>("reason", Sanitize(reason)));
        Increment(key);
    }

    public IReadOnlyDictionary<string, long> Snapshot() =>
        _snapshot.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);

    private void Increment(string key) => _snapshot.AddOrUpdate(key, 1, (_, n) => n + 1);

    private static string Normalize(string prefix, string reason) =>
        $"{prefix}.{Sanitize(reason)}";

    /// <summary>Keep labels short and free of secrets / PII payloads.</summary>
    private static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "unknown";
        var s = value.Trim().ToLowerInvariant();
        if (s.Length > 64) s = s[..64];
        foreach (var c in s)
        {
            if (char.IsLetterOrDigit(c) || c is '_' or '-' or '.') continue;
            s = new string(s.Select(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-' or '.' ? ch : '_').ToArray());
            break;
        }
        return s;
    }
}
