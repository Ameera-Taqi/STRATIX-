using Stratix.Domain.Enums;

namespace Stratix.Domain.Services;

public static class RiskLevelCalculator
{
    public static RiskLevel Calculate(RiskImpact impact, RiskProbability probability)
    {
        if (impact == RiskImpact.HIGH && probability == RiskProbability.HIGH) return RiskLevel.CRITICAL;
        if (impact == RiskImpact.HIGH && probability == RiskProbability.MEDIUM) return RiskLevel.HIGH;
        if (impact == RiskImpact.MEDIUM && probability == RiskProbability.MEDIUM) return RiskLevel.MEDIUM;
        if (impact == RiskImpact.LOW && probability == RiskProbability.LOW) return RiskLevel.LOW;
        if (impact == RiskImpact.HIGH || probability == RiskProbability.HIGH) return RiskLevel.HIGH;
        if (impact == RiskImpact.MEDIUM || probability == RiskProbability.MEDIUM) return RiskLevel.MEDIUM;
        return RiskLevel.LOW;
    }
}
