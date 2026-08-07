using Stratix.Domain.Entities;
using Stratix.Domain.Enums;

namespace Stratix.Domain.Workflow;

/// <summary>
/// After <see cref="EmployeeEvaluationStatus.APPROVED"/>, results / overall score / adjusted values
/// are immutable except via formal <c>Reopen Evaluation</c> (reason + audit).
/// </summary>
public static class EvaluationLockPolicy
{
    public static bool IsLocked(EmployeeEvaluationStatus status) =>
        status == EmployeeEvaluationStatus.APPROVED;

    public static void EnsureNotLocked(EmployeeEvaluation eval)
    {
        if (IsLocked(eval.Status))
            throw new InvalidOperationException(
                "Approved evaluations are locked. Use Reopen Evaluation with a reason to unlock.");
    }

    /// <summary>Recalculate / replace results — not allowed while APPROVED or IN_REVIEW.</summary>
    public static void EnsureCanRecalculate(EmployeeEvaluation eval)
    {
        EnsureNotLocked(eval);
        if (eval.Status == EmployeeEvaluationStatus.IN_REVIEW)
            throw new InvalidOperationException(
                "In-review evaluations cannot be recalculated. Approve, reject, or reopen after approval.");
    }

    /// <summary>Adjust scores/values during draft/review — never while APPROVED.</summary>
    public static void EnsureCanAdjust(EmployeeEvaluation eval) => EnsureNotLocked(eval);

    public static void EnsureCanReopen(EmployeeEvaluation eval, string? reason)
    {
        if (eval.Status != EmployeeEvaluationStatus.APPROVED)
            throw new InvalidOperationException("Only APPROVED evaluations can be reopened.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reopen reason is required.");
    }
}
