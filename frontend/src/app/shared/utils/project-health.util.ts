import { ProjectRow } from '../../core/data/mock-data';

export type HealthStatus = 'HEALTHY' | 'WARNING' | 'CRITICAL';

/** Weights / caps — keep in sync with ProjectHealthCalculator.cs */
export const HEALTH_PROGRESS_WEIGHT = 0.35;
export const HEALTH_ON_TIME_WEIGHT = 0.35;
export const HEALTH_DELAYED_WEIGHT = 0.15;
export const HEALTH_BLOCKED_WEIGHT = 0.15;
export const HEALTH_CRITICAL_POINTS_EACH = 15;
export const HEALTH_CRITICAL_PENALTY_CAP = 45;

export interface ProjectHealthFactors {
  progress: number;
  onTimeRate: number;
  delayedRate: number;
  blockedRate: number;
  criticalRiskPenalty: number;
  /** Diagnostics (raw counts — not used directly in the score). */
  onTimeTasks: number;
  delayedTasks: number;
  blockedTasks: number;
  criticalRisks: number;
  totalTasks: number;
  completedTasks: number;
}

export interface ProjectHealthResult {
  projectId: number;
  projectName: string;
  score: number;
  status: HealthStatus;
  progress: number;
  trend: 'up' | 'down' | 'stable';
  noteKey: string;
  factors: ProjectHealthFactors;
}

export interface HealthComputeInput {
  project: ProjectRow;
  totalTasks: number;
  completedTasks: number;
  onTimeCompletedTasks: number;
  delayedTasks: number;
  blockedTasks: number;
  criticalRisks: number;
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}

function roundPct(value: number): number {
  return Math.round(value * 10) / 10;
}

function statusFromScore(score: number, factors: ProjectHealthFactors): HealthStatus {
  if (score >= 70) return 'HEALTHY';
  if (score >= 45) return 'WARNING';
  const hasRealTrouble =
    factors.delayedTasks > 0 || factors.criticalRisks > 0 || factors.blockedTasks > 0;
  return hasRealTrouble ? 'CRITICAL' : 'WARNING';
}

function noteKeyFromFactors(factors: ProjectHealthFactors): string {
  if (factors.criticalRisks > 0) return 'health.noteRisks';
  if (factors.delayedTasks > 0) return 'health.noteOverdueTasks';
  if (factors.blockedTasks > 0) return 'health.noteBlocked';
  if (factors.totalTasks === 0) return 'health.noteNoTasks';
  return 'health.noteStable';
}

function trendFromScore(score: number, factors: ProjectHealthFactors): 'up' | 'down' | 'stable' {
  if (score >= 70 && factors.delayedTasks === 0 && factors.blockedTasks === 0) return 'up';
  if (score < 45 || factors.criticalRisks > 1) return 'down';
  return 'stable';
}

/**
 * Size-normalized health (0–100):
 * 0.35·Progress + 0.35·OnTimeRate + 0.15·(100−DelayedRate) + 0.15·(100−BlockedRate) − CriticalPenalty
 */
export function computeProjectHealth(input: HealthComputeInput): ProjectHealthResult {
  const progress = clamp(input.project.progress, 0, 100);
  const total = Math.max(0, input.totalTasks);
  const completed = Math.max(0, input.completedTasks);

  const onTimeRate =
    completed <= 0 ? 100 : clamp(roundPct((input.onTimeCompletedTasks * 100) / completed), 0, 100);
  const delayedRate =
    total <= 0 ? 0 : clamp(roundPct((input.delayedTasks * 100) / total), 0, 100);
  const blockedRate =
    total <= 0 ? 0 : clamp(roundPct((input.blockedTasks * 100) / total), 0, 100);
  const criticalRiskPenalty = clamp(
    input.criticalRisks * HEALTH_CRITICAL_POINTS_EACH,
    0,
    HEALTH_CRITICAL_PENALTY_CAP,
  );

  const factors: ProjectHealthFactors = {
    progress,
    onTimeRate,
    delayedRate,
    blockedRate,
    criticalRiskPenalty,
    onTimeTasks: input.onTimeCompletedTasks,
    delayedTasks: input.delayedTasks,
    blockedTasks: input.blockedTasks,
    criticalRisks: input.criticalRisks,
    totalTasks: total,
    completedTasks: completed,
  };

  const score =
    total <= 0
      ? roundPct(progress)
      : clamp(
          roundPct(
            progress * HEALTH_PROGRESS_WEIGHT +
              onTimeRate * HEALTH_ON_TIME_WEIGHT +
              (100 - delayedRate) * HEALTH_DELAYED_WEIGHT +
              (100 - blockedRate) * HEALTH_BLOCKED_WEIGHT -
              criticalRiskPenalty,
          ),
          0,
          100,
        );

  return {
    projectId: input.project.id,
    projectName: input.project.name,
    score,
    status: statusFromScore(score, factors),
    progress: input.project.progress,
    trend: trendFromScore(score, factors),
    noteKey: noteKeyFromFactors(factors),
    factors,
  };
}

export function healthStatusClass(status: HealthStatus): string {
  const map: Record<HealthStatus, string> = {
    HEALTHY: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300',
    WARNING: 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300',
    CRITICAL: 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300',
  };
  return map[status];
}

export const HEALTH_COLORS: Record<HealthStatus, string> = {
  HEALTHY: '#4E9B92',
  WARNING: '#E0B45C',
  CRITICAL: '#E07A7A',
};
