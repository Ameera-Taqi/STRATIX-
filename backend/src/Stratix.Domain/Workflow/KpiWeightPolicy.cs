using Stratix.Domain.Enums;

namespace Stratix.Domain.Workflow;

/// <summary>
/// Active KPI weights must form a complete 100% allocation before a period opens or Calculate runs.
/// Individual weights must be in (0, 100].
/// </summary>
public static class KpiWeightPolicy
{
    public const decimal RequiredTotal = 100m;
    public const decimal MinWeight = 0.01m;
    public const decimal MaxWeight = 100m;

    public static void EnsureValidWeight(decimal weight)
    {
        if (weight <= 0)
            throw new ArgumentException("KPI weight must be greater than 0 (negative or zero weights are not allowed).");
        if (weight > MaxWeight)
            throw new ArgumentException("KPI weight cannot exceed 100.");
    }

    /// <summary>
    /// Validates that each role bucket (including the org-wide null role) of active KPIs sums to 100.
    /// </summary>
    public static void EnsureActiveWeightsTotalOneHundred(
        IEnumerable<(string Code, decimal Weight, UserRole? AppliesToRole, bool IsActive)> definitions)
    {
        var active = definitions.Where(d => d.IsActive).ToList();
        if (active.Count == 0)
            throw new InvalidOperationException(
                "No active KPI definitions. Add active KPIs whose weights sum to 100% before opening a period or calculating.");

        foreach (var group in active.GroupBy(d => d.AppliesToRole))
        {
            var total = group.Sum(d => d.Weight);
            var roleLabel = group.Key?.ToString() ?? "ALL_ROLES";
            if (total != RequiredTotal)
                throw new InvalidOperationException(
                    $"Active KPI weights for {roleLabel} must total exactly 100% (currently {total}%).");
        }
    }

    public static void EnsureNoDuplicateCodeForRole(
        IEnumerable<(long Id, string Code, UserRole? AppliesToRole, bool IsActive)> definitions,
        long? excludingId,
        string code,
        UserRole? appliesToRole)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var conflict = definitions.FirstOrDefault(d =>
            d.Id != (excludingId ?? -1) &&
            d.IsActive &&
            string.Equals(d.Code, normalized, StringComparison.OrdinalIgnoreCase) &&
            d.AppliesToRole == appliesToRole);

        if (conflict.Code is not null)
            throw new InvalidOperationException(
                $"An active KPI with code '{normalized}' already exists for role {(appliesToRole?.ToString() ?? "ALL_ROLES")}.");
    }
}
