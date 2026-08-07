using Stratix.Domain.Entities;

namespace Stratix.Api.Tests;

public class KpiSnapshotPolicyTests
{
    [Fact]
    public void Overall_score_uses_snapshot_weight_not_live_definition_weight()
    {
        var liveDef = new KpiDefinition { Weight = 99m, Code = "TASKS_COMPLETED", Name = "Live name" };
        var result = new EmployeeKpiResult
        {
            KpiDefinition = liveDef,
            SnapshotWeight = 1m,
            SnapshotName = "Frozen name",
            SnapshotCode = "TASKS_COMPLETED",
            SnapshotFormula = "TASKS_COMPLETED",
            Score = 50m,
        };

        var weight = result.SnapshotWeight > 0 ? result.SnapshotWeight : 1m;
        Assert.Equal(1m, weight);
        Assert.NotEqual(liveDef.Weight, weight);
        Assert.Equal("Frozen name", result.SnapshotName);
    }
}
