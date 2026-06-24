import { RiskHeatMapCell, RiskImpact, RiskLevel, RiskProbability } from '../../core/models/risk.model';

const MATRIX: Record<RiskImpact, Record<RiskProbability, RiskLevel>> = {
  LOW: { LOW: 'LOW', MEDIUM: 'LOW', HIGH: 'MEDIUM' },
  MEDIUM: { LOW: 'LOW', MEDIUM: 'MEDIUM', HIGH: 'HIGH' },
  HIGH: { LOW: 'MEDIUM', MEDIUM: 'HIGH', HIGH: 'CRITICAL' },
};

export function calculateRiskLevel(impact: RiskImpact, probability: RiskProbability): RiskLevel {
  return MATRIX[impact][probability];
}

export function buildEmptyHeatMap(): RiskHeatMapCell[] {
  const impacts: RiskImpact[] = ['LOW', 'MEDIUM', 'HIGH'];
  const probabilities: RiskProbability[] = ['LOW', 'MEDIUM', 'HIGH'];
  return impacts.flatMap((impact) =>
    probabilities.map((probability) => ({
      impact,
      probability,
      riskLevel: calculateRiskLevel(impact, probability),
      count: 0,
    })),
  );
}

export function aggregateHeatMap(
  risks: { impact: RiskImpact; probability: RiskProbability }[],
): RiskHeatMapCell[] {
  const cells = buildEmptyHeatMap();
  for (const risk of risks) {
    const cell = cells.find((c) => c.impact === risk.impact && c.probability === risk.probability);
    if (cell) cell.count += 1;
  }
  return cells;
}

export function riskLevelClass(level: RiskLevel): string {
  const map: Record<RiskLevel, string> = {
    LOW: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300',
    MEDIUM: 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300',
    HIGH: 'bg-orange-100 text-orange-700 dark:bg-orange-900/40 dark:text-orange-300',
    CRITICAL: 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300',
  };
  return map[level];
}

export function riskStatusClass(status: string): string {
  const map: Record<string, string> = {
    OPEN: 'bg-red-50 text-red-700 dark:bg-red-900/30 dark:text-red-300',
    MITIGATING: 'bg-amber-50 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300',
    CLOSED: 'bg-emerald-50 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-300',
  };
  return map[status] ?? 'bg-slate-100 text-slate-600';
}
