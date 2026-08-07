using Stratix.Domain.Enums;

namespace Stratix.Application.Common;

/// <summary>
/// Plan code mapping helpers.
/// Source of truth: <c>subscriptions.plan_code</c> → quotas from <c>subscription_plans</c>.
/// <c>organizations.subscription_plan</c> is a denormalized FREE/PRO/ENTERPRISE mirror.
/// </summary>
public static class PlanCodes
{
    public static SubscriptionPlan ToOrganizationPlan(string? planCode) =>
        (planCode ?? "").Trim().ToUpperInvariant() switch
        {
            "ENTERPRISE" => SubscriptionPlan.ENTERPRISE,
            "PRO" or "TRIAL" => SubscriptionPlan.PRO,
            _ => SubscriptionPlan.FREE,
        };

    public static string ToTierName(string? planCode) =>
        (planCode ?? "").Trim().ToUpperInvariant() switch
        {
            "ENTERPRISE" => "Enterprise",
            "PRO" or "TRIAL" => "Professional",
            _ => "Starter",
        };

    public static string FromOrganizationPlan(SubscriptionPlan plan) => plan.ToString();
}
