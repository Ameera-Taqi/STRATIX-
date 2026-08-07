using Stratix.Domain.Enums;
using Stratix.Domain.Workflow;

namespace Stratix.Api.Tests;

public class KpiWeightPolicyTests
{
    [Fact]
    public void Rejects_negative_zero_and_over_100_weights()
    {
        Assert.Throws<ArgumentException>(() => KpiWeightPolicy.EnsureValidWeight(0));
        Assert.Throws<ArgumentException>(() => KpiWeightPolicy.EnsureValidWeight(-1));
        Assert.Throws<ArgumentException>(() => KpiWeightPolicy.EnsureValidWeight(100.01m));
        KpiWeightPolicy.EnsureValidWeight(100m);
        KpiWeightPolicy.EnsureValidWeight(25m);
    }

    [Fact]
    public void Active_weights_must_total_exactly_100_per_role_bucket()
    {
        Assert.Throws<InvalidOperationException>(() =>
            KpiWeightPolicy.EnsureActiveWeightsTotalOneHundred([
                ("A", 40m, null, true),
                ("B", 40m, null, true),
            ]));

        KpiWeightPolicy.EnsureActiveWeightsTotalOneHundred([
            ("A", 40m, null, true),
            ("B", 60m, null, true),
            ("C", 10m, null, false), // inactive ignored
        ]);

        KpiWeightPolicy.EnsureActiveWeightsTotalOneHundred([
            ("A", 100m, UserRole.EMPLOYEE, true),
            ("B", 50m, UserRole.PROJECT_MANAGER, true),
            ("C", 50m, UserRole.PROJECT_MANAGER, true),
        ]);
    }

    [Fact]
    public void Duplicate_code_for_same_role_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() =>
            KpiWeightPolicy.EnsureNoDuplicateCodeForRole(
                [(1, "TASKS_COMPLETED", null, true)],
                excludingId: null,
                code: "TASKS_COMPLETED",
                appliesToRole: null));

        KpiWeightPolicy.EnsureNoDuplicateCodeForRole(
            [(1, "TASKS_COMPLETED", null, true)],
            excludingId: null,
            code: "TASKS_COMPLETED",
            appliesToRole: UserRole.EMPLOYEE);
    }
}
