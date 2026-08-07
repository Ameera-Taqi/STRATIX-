export type EvaluationPeriodStatus = 'DRAFT' | 'OPEN' | 'CLOSED' | string;
export type EmployeeEvaluationStatus =
  | 'DRAFT'
  | 'SUBMITTED'
  | 'IN_REVIEW'
  | 'APPROVED'
  | 'REJECTED'
  | string;

export interface EvaluationPeriod {
  id: number;
  name: string;
  startDate: string;
  endDate: string;
  status: EvaluationPeriodStatus;
  createdAt: string;
  updatedAt: string;
}

export interface EmployeeKpiResult {
  id: number;
  kpiDefinitionId: number;
  kpiCode: string;
  kpiName: string;
  snapshotWeight: number;
  snapshotTarget: number | null;
  snapshotFormula: string;
  snapshotHigherIsBetter: boolean;
  calculatedValue: number;
  adjustedValue: number | null;
  score: number;
  comment: string | null;
}

export interface EmployeeEvaluation {
  id: number;
  periodId: number;
  periodName: string;
  userId: number;
  userName: string;
  status: EmployeeEvaluationStatus;
  overallScore: number | null;
  notes: string | null;
  submittedAt: string | null;
  reviewedAt: string | null;
  approvedAt: string | null;
  rejectionReason: string | null;
  reopenReason: string | null;
  results: EmployeeKpiResult[];
}

export interface AdjustKpiResultRequest {
  adjustedValue?: number | null;
  score?: number | null;
  comment?: string | null;
}

/** Manager-entered scores (Quality, Collaboration, MANAGER_SCORE). */
export function isManagerKpi(result: EmployeeKpiResult): boolean {
  const f = (result.snapshotFormula || '').toUpperCase();
  const c = (result.kpiCode || '').toUpperCase();
  return (
    f === 'MANAGER_SCORE' ||
    f === 'QUALITY' ||
    f === 'COLLABORATION' ||
    c === 'QUALITY' ||
    c === 'COLLABORATION'
  );
}

/** System-calculated metrics shown read-only in the evaluation scorecard. */
export function isSystemKpi(result: EmployeeKpiResult): boolean {
  return !isManagerKpi(result);
}

export function evaluationStatusLabelKey(status: string): string {
  switch (status) {
    case 'DRAFT':
      return 'eval.status.draft';
    case 'SUBMITTED':
      return 'eval.status.submitted';
    case 'IN_REVIEW':
      return 'eval.status.underReview';
    case 'APPROVED':
      return 'eval.status.approved';
    case 'REJECTED':
      return 'eval.status.rejected';
    default:
      return 'eval.status.notStarted';
  }
}

export function scoreBandKey(score: number | null | undefined): string {
  if (score == null || Number.isNaN(score)) return 'eval.band.none';
  if (score >= 90) return 'eval.band.excellent';
  if (score >= 80) return 'eval.band.veryGood';
  if (score >= 70) return 'eval.band.good';
  if (score >= 60) return 'eval.band.fair';
  return 'eval.band.needsImprovement';
}

/** True when the employee may see the full transparent scorecard (not a private draft). */
export function isEmployeeVisibleStatus(status: string): boolean {
  return (
    status === 'SUBMITTED' ||
    status === 'IN_REVIEW' ||
    status === 'APPROVED' ||
    status === 'REJECTED'
  );
}

export interface KpiBreakdownRow {
  id: number;
  name: string;
  score: number;
  weight: number;
  contribution: number;
  isManager: boolean;
}

/**
 * Weighted contribution matching backend OverallScore:
 * contribution = score × weight / Σweights  (equals score × weight% when weights sum to 100).
 */
export function buildKpiBreakdown(
  results: EmployeeKpiResult[],
  scoreOverrides?: Record<number, number>,
): KpiBreakdownRow[] {
  const weightSum = results.reduce((s, r) => s + (r.snapshotWeight > 0 ? r.snapshotWeight : 1), 0);
  return [...results]
    .map((r) => {
      const weight = r.snapshotWeight > 0 ? r.snapshotWeight : 1;
      const score =
        scoreOverrides?.[r.id] != null && isManagerKpi(r)
          ? scoreOverrides[r.id]
          : r.score;
      const contribution = weightSum <= 0 ? 0 : (score * weight) / weightSum;
      return {
        id: r.id,
        name: r.kpiName,
        score,
        weight,
        contribution,
        isManager: isManagerKpi(r),
      };
    })
    .sort((a, b) => b.weight - a.weight || a.name.localeCompare(b.name));
}

export function formatKpiNumber(n: number, digits = 2): string {
  if (Number.isInteger(n) || Math.abs(n - Math.round(n)) < 1e-9) return String(Math.round(n));
  return n.toFixed(digits);
}
