export interface TaskProgressInput {
  status: string;
  stageId?: number | null;
  /** Effort weight in hours; defaults to 1 when missing/zero. */
  estimatedHours?: number | null;
}

export interface EffortBreakdown {
  progress: number;
  taskCount: number;
  /** Total estimated hours (effort weights). */
  effortTotal: number;
  /** Completed effort hours (DONE tasks only). */
  effortDone: number;
}

const DEFAULT_EFFORT = 1;

function effortOf(task: TaskProgressInput): number {
  const hours = Number(task.estimatedHours);
  return hours > 0 ? hours : DEFAULT_EFFORT;
}

function isDone(status: string): boolean {
  return status === 'DONE';
}

function roundHours(value: number): number {
  return Math.round(value * 10) / 10;
}

/** Full effort breakdown used by read-only progress UI. */
export function effortBreakdownFromTasks(tasks: TaskProgressInput[]): EffortBreakdown {
  if (tasks.length === 0) {
    return { progress: 0, taskCount: 0, effortTotal: 0, effortDone: 0 };
  }
  let total = 0;
  let done = 0;
  for (const t of tasks) {
    const e = effortOf(t);
    total += e;
    if (isDone(t.status)) done += e;
  }
  const progress = total <= 0 ? 0 : Math.round((done / total) * 100);
  return {
    progress,
    taskCount: tasks.length,
    effortTotal: roundHours(total),
    effortDone: roundHours(done),
  };
}

/** Stage progress = done effort / total effort for tasks in the stage.
 * Empty stage (no tasks) → 0. Always derived — never a manual/unknown value.
 */
export function stageProgressFromTasks(tasks: TaskProgressInput[]): number {
  return effortBreakdownFromTasks(tasks).progress;
}

/**
 * Project progress from effort (same formula as backend ProjectProgressCalculator).
 * Prefer this over equal-weight task counts.
 *
 * Empty-set policy: no tasks → 0 (never null / NaN / divide-by-zero).
 * For a still-PLANNED project, 0% with no tasks is the normal expected state.
 */
export function progressFromTasks(tasks: TaskProgressInput[]): number {
  return effortBreakdownFromTasks(tasks).progress;
}

/**
 * Average of stage progress values.
 * Empty stages → 0 (defined), same empty-set policy as tasks.
 */
export function progressFromStages(stages: { progress: number }[]): number {
  if (stages.length === 0) return 0;
  return Math.round(stages.reduce((sum, s) => sum + s.progress, 0) / stages.length);
}
