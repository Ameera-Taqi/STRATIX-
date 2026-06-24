export interface TaskProgressInput {
  status: string;
  stageId?: number | null;
}

/** Project progress = percentage of tasks marked DONE. */
export function progressFromTasks(tasks: TaskProgressInput[]): number | null {
  if (tasks.length === 0) return null;
  const done = tasks.filter((t) => t.status === 'DONE').length;
  return Math.round((done / tasks.length) * 100);
}

export function progressFromStages(stages: { progress: number }[]): number | null {
  if (stages.length === 0) return null;
  return Math.round(stages.reduce((sum, s) => sum + s.progress, 0) / stages.length);
}

/** Stage progress from its linked tasks. */
export function stageProgressFromTasks(tasks: TaskProgressInput[]): number {
  if (tasks.length === 0) return 0;
  return progressFromTasks(tasks) ?? 0;
}
