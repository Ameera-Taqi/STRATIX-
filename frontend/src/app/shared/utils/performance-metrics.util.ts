import { EmployeeRow, TaskCard } from '../../core/data/mock-data';
import { ProjectRisk } from '../../core/models/risk.model';

export interface PerformanceMetrics {
  /** % of all tasks marked DONE */
  completionRate: number;
  /** % of tasks on track (done or not past due date) */
  onTimePct: number;
  /** Count of open tasks past due date */
  overdueTasks: number;
  /** Count of on-time / on-track tasks */
  onTimeTaskCount: number;
  /** Average employee KPI score (same formula as Team page) */
  teamProductivity: number;
  /** Portfolio-level task completion (same as completionRate) */
  avgTaskCompletion: number;
  /** 0–100 risk exposure score (higher = more risk impact) */
  riskImpact: number;
  totalTasks: number;
  completedTasks: number;
  openCriticalRisks: number;
  hasData: boolean;
}

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

/**
 * Portfolio-level performance metrics derived from tasks, team KPIs, and risks.
 * Task/on-time formulas match {@link computeEmployeeTaskStats} in employee-stats.util.ts.
 */
export function computePerformanceMetrics(
  tasks: TaskCard[],
  employees: EmployeeRow[],
  risks: ProjectRisk[],
): PerformanceMetrics {
  const today = todayIso();
  const totalTasks = tasks.length;
  const completedTasks = tasks.filter((t) => t.status === 'DONE').length;
  const completionRate =
    totalTasks === 0 ? 0 : Math.round((completedTasks / totalTasks) * 100);

  const onTimeTaskCount = tasks.filter(
    (t) => t.status === 'DONE' || !t.dueDate || t.dueDate >= today,
  ).length;
  const onTimePct =
    totalTasks === 0 ? 100 : Math.round((onTimeTaskCount / totalTasks) * 100);

  const overdueTasks = tasks.filter(
    (t) => t.status !== 'DONE' && t.dueDate && t.dueDate < today,
  ).length;

  const employeesWithActivity = employees.filter(
    (e) => (e.kpiScore ?? 0) > 0 || (e.tasksCompleted ?? 0) > 0,
  );
  const teamProductivity =
    employeesWithActivity.length === 0
      ? 0
      : Math.round(
          employeesWithActivity.reduce((sum, e) => sum + (e.kpiScore ?? 0), 0) /
            employeesWithActivity.length,
        );

  const openRisks = risks.filter((r) => r.status !== 'CLOSED');
  const openCriticalRisks = openRisks.filter((r) => r.riskLevel === 'CRITICAL').length;
  const openHighRisks = openRisks.filter((r) => r.riskLevel === 'HIGH').length;
  const riskImpact = Math.min(
    100,
    openCriticalRisks * 25 + openHighRisks * 10 + openRisks.length * 3,
  );

  const hasData = totalTasks > 0 || employees.length > 0;

  return {
    completionRate,
    onTimePct,
    overdueTasks,
    onTimeTaskCount,
    teamProductivity,
    avgTaskCompletion: completionRate,
    riskImpact,
    totalTasks,
    completedTasks,
    openCriticalRisks,
    hasData,
  };
}
