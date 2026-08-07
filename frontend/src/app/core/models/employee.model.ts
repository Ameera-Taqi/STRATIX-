export type EmployeeStatus = 'Active' | 'Inactive';

export interface EmployeeRow {
  id: number;
  name: string;
  email?: string;
  role: string;
  department: string;
  projects: number;
  completedTasks: number;
  status: EmployeeStatus;
  tasksCompleted?: number;
  delayedTasks?: number;
  onTimePct?: number;
  kpiScore?: number;
}
