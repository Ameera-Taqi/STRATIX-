export interface TaskProgressInput {
  status: string;
  stageId?: number | null;
  /** Effort weight in hours; defaults to 1 when missing/zero. */
  estimatedHours?: number | null;
}

const DEFAULT_EFFORT = 1;

function effortOf(task: TaskProgressInput): number {
  const hours = Number(task.estimatedHours);
  return hours > 0 ? hours : DEFAULT_EFFORT;
}

function isDone(status: string): boolean {
  return status === 'DONE';
}

/** Stage progress = done effort / total effort for tasks in the stage. */
export function stageProgressFromTasks(tasks: TaskProgressInput[]): number {
  if (tasks.length === 0) return 0;
  let total = 0;
  let done = 0;
  for (const t of tasks) {
    const e = effortOf(t);
    total += e;
    if (isDone(t.status)) done += e;
  }
  if (total <= 0) return 0;
  return Math.round((done / total) * 100);
}

/**
 * Project progress from effort (same formula as backend ProjectProgressCalculator).
 * Prefer this over equal-weight task counts.
 */
export function progressFromTasks(tasks: TaskProgressInput[]): number | null {
  if (tasks.length === 0) return null;
  return stageProgressFromTasks(tasks);
}

/** @deprecated Prefer effort-weighted project progress; kept for stage-average fallbacks. */
export function progressFromStages(stages: { progress: number }[]): number | null {
  if (stages.length === 0) return null;
  return Math.round(stages.reduce((sum, s) => sum + s.progress, 0) / stages.length);
}
