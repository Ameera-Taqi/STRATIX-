using Stratix.Domain.Entities;
using Stratix.Domain.Enums;
using Stratix.Domain.Workflow;

namespace Stratix.Api.Tests;

public class EvaluationLockPolicyTests
{
    [Fact]
    public void Approved_evaluation_is_locked_from_recalculate_and_adjust()
    {
        var eval = new EmployeeEvaluation { Status = EmployeeEvaluationStatus.APPROVED };
        Assert.True(EvaluationLockPolicy.IsLocked(eval.Status));
        Assert.Throws<InvalidOperationException>(() => EvaluationLockPolicy.EnsureNotLocked(eval));
        Assert.Throws<InvalidOperationException>(() => EvaluationLockPolicy.EnsureCanRecalculate(eval));
        Assert.Throws<InvalidOperationException>(() => EvaluationLockPolicy.EnsureCanAdjust(eval));
    }

    [Fact]
    public void Reopen_requires_approved_status_and_reason()
    {
        var draft = new EmployeeEvaluation { Status = EmployeeEvaluationStatus.DRAFT };
        Assert.Throws<InvalidOperationException>(() => EvaluationLockPolicy.EnsureCanReopen(draft, "fix"));

        var approved = new EmployeeEvaluation { Status = EmployeeEvaluationStatus.APPROVED };
        Assert.Throws<ArgumentException>(() => EvaluationLockPolicy.EnsureCanReopen(approved, "  "));
        EvaluationLockPolicy.EnsureCanReopen(approved, "Correct scoring error");
    }

    [Fact]
    public void Draft_can_recalculate_and_adjust()
    {
        var eval = new EmployeeEvaluation { Status = EmployeeEvaluationStatus.DRAFT };
        EvaluationLockPolicy.EnsureCanRecalculate(eval);
        EvaluationLockPolicy.EnsureCanAdjust(eval);
    }
}
