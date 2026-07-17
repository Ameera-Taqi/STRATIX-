import { ProjectRow } from '../../core/data/mock-data';

export type HealthStatus = 'HEALTHY' | 'WARNING' | 'CRITICAL';

export interface ProjectHealthFactors {
  progress: number;
  onTimeTasks: number;
  delayedTasks: number;
  criticalRisks: number;
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
  onTimeTasks: number;
  delayedTasks: number;
  criticalRisks: number;
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}

function statusFromScore(score: number, factors: ProjectHealthFactors): HealthStatus {
  if (score >= 70) return 'HEALTHY';
  if (score >= 45) return 'WARNING';
  // A low score alone doesn't mean trouble — a brand-new, not-yet-started project
  // scores low simply because there's nothing to measure. Reserve CRITICAL (red) for
  // projects with real warning signals: overdue tasks or open critical risks.
  const hasRealTrouble = factors.delayedTasks > 0 || factors.criticalRisks > 0;
  return hasRealTrouble ? 'CRITICAL' : 'WARNING';
}

function noteKeyFromFactors(factors: ProjectHealthFactors): string {
  if (factors.criticalRisks > 0) return 'health.noteRisks';
  if (factors.delayedTasks > 0) return 'health.noteOverdueTasks';
  if (factors.onTimeTasks === 0 && factors.delayedTasks === 0) return 'health.noteNoTasks';
  return 'health.noteStable';
}

function trendFromScore(score: number, factors: ProjectHealthFactors): 'up' | 'down' | 'stable' {
  if (score >= 70 && factors.delayedTasks === 0) return 'up';
  if (score < 45 || factors.criticalRisks > 1) return 'down';
  return 'stable';
}

/** Health Score = progress + on-time tasks − delayed tasks − open critical risks */
export function computeProjectHealth(input: HealthComputeInput): ProjectHealthResult {
  const factors: ProjectHealthFactors = {
    progress: input.project.progress,
    onTimeTasks: input.onTimeTasks,
    delayedTasks: input.delayedTasks,
    criticalRisks: input.criticalRisks,
  };

  const score = clamp(
    factors.progress + factors.onTimeTasks - factors.delayedTasks - factors.criticalRisks,
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
  HEALTHY: '#10b981',
  WARNING: '#f59e0b',
  CRITICAL: '#ef4444',
};
