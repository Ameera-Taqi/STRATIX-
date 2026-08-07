namespace Stratix.Application.Observability;

/// <summary>Application counters for ops dashboards (no sensitive payloads).</summary>
public interface IStratixMetrics
{
    void RecordFailedLogin(string reason);
    void RecordSubscriptionRejection(string reason);
    void RecordAiAnalysisFailure(string reason);
    void RecordStorageFailure(string operation, string reason);

    IReadOnlyDictionary<string, long> Snapshot();
}
