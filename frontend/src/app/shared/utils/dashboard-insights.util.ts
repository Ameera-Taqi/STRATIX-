import { TaskCard, ProjectRow } from '../../core/data/mock-data';
import { EmployeeRow } from '../../core/data/mock-data';
import {
  BudgetStatusItem,
  DepartmentPerformanceItem,
  EmployeeLoadItem,
  OverdueTaskItem,
} from '../../core/data/dashboard-insights';

const DEFAULT_TASK_CAPACITY = 8;

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

function daysBetween(from: string, to: string): number {
  const ms = new Date(to).getTime() - new Date(from).getTime();
  return Math.max(0, Math.floor(ms / 86_400_000));
}

export function expectedProgress(
  startDate: string,
  endDate: string,
  today = todayIso(),
): number {
  if (!startDate || !endDate) return 0;
  if (today <= startDate) return 0;
  if (today >= endDate) return 100;
  const total = daysBetween(startDate, endDate);
  if (total === 0) return 100;
  return Math.round((daysBetween(startDate, today) / total) * 100);
}

export function computeOverdueTasks(tasks: TaskCard[]): OverdueTaskItem[] {
  const today = todayIso();
  return tasks
    .filter((t) => t.status !== 'DONE' && t.dueDate && t.dueDate < today)
    .map((t) => ({
      id: t.id,
      title: t.title,
      projectName: t.projectName,
      assignee: t.assignee,
      dueDate: t.dueDate,
      daysOverdue: daysBetween(t.dueDate, today),
      priority: t.priority,
    }))
    .sort((a, b) => b.daysOverdue - a.daysOverdue);
}

export function computeEmployeeLoad(
  employees: EmployeeRow[],
  tasks: TaskCard[],
): EmployeeLoadItem[] {
  const today = todayIso();

  return employees
    .map((employee) => {
      const activeTasks = tasks.filter(
        (t) =>
          t.status !== 'DONE' &&
          (t.assigneeId === employee.id ||
            (t.assigneeId == null &&
              t.assignee.trim().toLowerCase() === employee.name.trim().toLowerCase())),
      ).length;
      const loadPct = Math.round((activeTasks / DEFAULT_TASK_CAPACITY) * 100);
      const status: EmployeeLoadItem['status'] =
        loadPct > 100 ? 'OVERLOADED' : loadPct >= 80 ? 'HIGH' : 'NORMAL';

      return {
        id: employee.id,
        name: employee.name,
        department: employee.department,
        loadPct,
        activeTasks,
        capacity: DEFAULT_TASK_CAPACITY,
        status,
      };
    })
    .sort((a, b) => b.loadPct - a.loadPct);
}

export function computeDepartmentPerformance(
  projects: ProjectRow[],
  tasks: TaskCard[],
): DepartmentPerformanceItem[] {
  const today = todayIso();
  const byDept = new Map<string, { projects: ProjectRow[]; tasks: TaskCard[] }>();

  for (const project of projects) {
    const dept = project.department || 'Unknown';
    const entry = byDept.get(dept) ?? { projects: [], tasks: [] };
    entry.projects.push(project);
    byDept.set(dept, entry);
  }

  for (const task of tasks) {
    const project = projects.find((p) => p.id === task.projectId);
    if (!project) continue;
    const dept = project.department || 'Unknown';
    const entry = byDept.get(dept) ?? { projects: [], tasks: [] };
    entry.tasks.push(task);
    byDept.set(dept, entry);
  }

  return [...byDept.entries()]
    .map(([department, { projects: deptProjects, tasks: deptTasks }]) => {
      const score =
        deptProjects.length === 0
          ? 0
          : Math.round(
              deptProjects.reduce((sum, p) => sum + p.progress, 0) / deptProjects.length,
            );
      const onTime = deptTasks.filter(
        (t) => t.status === 'DONE' || !t.dueDate || t.dueDate >= today,
      ).length;
      const onTimePct =
        deptTasks.length === 0 ? 100 : Math.round((onTime / deptTasks.length) * 100);
      const delayed = deptTasks.filter(
        (t) => t.status !== 'DONE' && t.dueDate && t.dueDate < today,
      ).length;
      const trend = delayed === 0 ? 2 : delayed <= 2 ? 0 : -3;

      return {
        department,
        score,
        projects: deptProjects.length,
        onTimePct,
        trend,
      };
    })
    .sort((a, b) => b.score - a.score);
}

/** Schedule variance per project (reuses budget chart shape; values are % × 1000 for chart scale). */
export function computeScheduleStatus(projects: ProjectRow[]): BudgetStatusItem[] {
  const today = todayIso();

  return projects.map((project) => {
    const expected = expectedProgress(project.startDate, project.endDate, today);
    const spent = project.progress;
    const variance = spent - expected;

    let forecast = spent;
    if (project.startDate && project.endDate && today < project.endDate) {
      const elapsed = daysBetween(project.startDate, today);
      const total = daysBetween(project.startDate, project.endDate);
      if (elapsed > 0 && spent > 0) {
        forecast = Math.min(100, Math.round((spent / elapsed) * total));
      }
    } else if (today >= project.endDate) {
      forecast = spent;
    }

    let status: BudgetStatusItem['status'] = 'ON_TRACK';
    if (variance < -15) status = 'OVER_BUDGET';
    else if (variance < -5) status = 'WARNING';

    return {
      projectId: project.id,
      projectName: project.name,
      budget: expected * 1_000,
      spent: spent * 1_000,
      forecast: forecast * 1_000,
      status,
    };
  });
}
