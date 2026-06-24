import { ProjectRow, TaskCard } from '../../core/data/mock-data';

export interface EmployeeTaskStats {
  projects: number;
  completedTasks: number;
  tasksCompleted: number;
  delayedTasks: number;
  onTimePct: number;
  kpiScore: number;
}

export function tasksForEmployee(
  employeeId: number,
  employeeName: string,
  tasks: TaskCard[],
): TaskCard[] {
  const name = employeeName.trim().toLowerCase();
  return tasks.filter(
    (t) =>
      t.assigneeId === employeeId ||
      (t.assigneeId == null && t.assignee.trim().toLowerCase() === name),
  );
}

export function projectsForEmployee(
  employeeId: number,
  employeeName: string,
  tasks: TaskCard[],
  projects: ProjectRow[],
): Set<number> {
  const name = employeeName.trim().toLowerCase();
  const fromTasks = new Set(tasksForEmployee(employeeId, employeeName, tasks).map((t) => t.projectId));
  for (const project of projects) {
    if (project.managerId === employeeId) {
      fromTasks.add(project.id);
      continue;
    }
    if (project.manager.trim().toLowerCase() === name) {
      fromTasks.add(project.id);
    }
  }
  return fromTasks;
}

export function computeEmployeeTaskStats(
  employeeId: number,
  employeeName: string,
  tasks: TaskCard[],
  projects: ProjectRow[] = [],
): EmployeeTaskStats {
  const empTasks = tasksForEmployee(employeeId, employeeName, tasks);
  const today = new Date().toISOString().slice(0, 10);
  const completed = empTasks.filter((t) => t.status === 'DONE').length;
  const delayed = empTasks.filter((t) => t.dueDate < today && t.status !== 'DONE').length;
  const onTime = empTasks.filter((t) => t.status === 'DONE' || t.dueDate >= today).length;
  const onTimePct = empTasks.length === 0 ? 100 : Math.round((onTime / empTasks.length) * 100);
  const projectCount = projectsForEmployee(employeeId, employeeName, tasks, projects).size;
  const completionRate = empTasks.length === 0 ? 0 : Math.round((completed / empTasks.length) * 100);
  const kpiScore =
    empTasks.length === 0 ? 0 : Math.min(100, Math.round(completionRate * 0.65 + onTimePct * 0.35));

  return {
    projects: projectCount,
    completedTasks: completed,
    tasksCompleted: completed,
    delayedTasks: delayed,
    onTimePct,
    kpiScore,
  };
}
