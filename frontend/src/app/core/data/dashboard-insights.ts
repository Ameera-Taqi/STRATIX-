export type HealthStatus = 'HEALTHY' | 'WARNING' | 'CRITICAL';

export interface ProjectHealthItem {
  projectId: number;
  projectName: string;
  score: number;
  status: HealthStatus;
  progress: number;
  trend: 'up' | 'down' | 'stable';
  noteKey: string;
}

export interface OverdueTaskItem {
  id: number;
  title: string;
  projectName: string;
  assignee: string;
  dueDate: string;
  daysOverdue: number;
  priority: string;
}

export interface EmployeeLoadItem {
  id: number;
  name: string;
  department: string;
  loadPct: number;
  activeTasks: number;
  capacity: number;
  status: 'NORMAL' | 'HIGH' | 'OVERLOADED';
}

export interface DepartmentPerformanceItem {
  department: string;
  score: number;
  projects: number;
  onTimePct: number;
  trend: number;
}

export interface BudgetStatusItem {
  projectId: number;
  projectName: string;
  budget: number;
  spent: number;
  forecast: number;
  status: 'ON_TRACK' | 'WARNING' | 'OVER_BUDGET';
}

export const MOCK_PROJECT_HEALTH: ProjectHealthItem[] = [
  { projectId: 1, projectName: 'ERP Rollout', score: 78, status: 'HEALTHY', progress: 72, trend: 'up', noteKey: 'dashboard.healthNoteStable' },
  { projectId: 2, projectName: 'Mobile App v2', score: 61, status: 'WARNING', progress: 45, trend: 'down', noteKey: 'dashboard.healthNoteScope' },
  { projectId: 3, projectName: 'Data Migration', score: 88, status: 'HEALTHY', progress: 90, trend: 'up', noteKey: 'dashboard.healthNoteOnTrack' },
  { projectId: 4, projectName: 'Security Audit', score: 38, status: 'CRITICAL', progress: 30, trend: 'down', noteKey: 'dashboard.healthNoteDelayed' },
];

export const MOCK_OVERDUE_TASKS: OverdueTaskItem[] = [
  { id: 1, title: 'API integration', projectName: 'ERP Rollout', assignee: 'Sara Ali', dueDate: '2026-06-10', daysOverdue: 3, priority: 'HIGH' },
  { id: 3, title: 'Schema validation', projectName: 'Data Migration', assignee: 'Lina Noor', dueDate: '2026-06-08', daysOverdue: 5, priority: 'URGENT' },
  { id: 2, title: 'UI mockups', projectName: 'Mobile App v2', assignee: 'Omar Hassan', dueDate: '2026-06-05', daysOverdue: 8, priority: 'MEDIUM' },
];

export const MOCK_EMPLOYEE_LOAD: EmployeeLoadItem[] = [
  { id: 1, name: 'Sara Ali', department: 'IT', loadPct: 92, activeTasks: 11, capacity: 12, status: 'HIGH' },
  { id: 2, name: 'Omar Hassan', department: 'Product', loadPct: 78, activeTasks: 7, capacity: 9, status: 'NORMAL' },
  { id: 3, name: 'Lina Noor', department: 'Operations', loadPct: 64, activeTasks: 5, capacity: 8, status: 'NORMAL' },
  { id: 4, name: 'Khalid Fahad', department: 'IT', loadPct: 105, activeTasks: 9, capacity: 8, status: 'OVERLOADED' },
];

export const MOCK_DEPARTMENT_PERFORMANCE: DepartmentPerformanceItem[] = [
  { department: 'IT', score: 84, projects: 5, onTimePct: 91, trend: 3 },
  { department: 'Product', score: 79, projects: 3, onTimePct: 86, trend: -2 },
  { department: 'Operations', score: 92, projects: 2, onTimePct: 96, trend: 5 },
  { department: 'Finance', score: 88, projects: 1, onTimePct: 94, trend: 1 },
];

export const MOCK_BUDGET_STATUS: BudgetStatusItem[] = [
  { projectId: 1, projectName: 'ERP Rollout', budget: 420000, spent: 305000, forecast: 398000, status: 'ON_TRACK' },
  { projectId: 2, projectName: 'Mobile App v2', budget: 180000, spent: 142000, forecast: 195000, status: 'WARNING' },
  { projectId: 3, projectName: 'Data Migration', budget: 95000, spent: 88000, forecast: 92000, status: 'ON_TRACK' },
  { projectId: 4, projectName: 'Security Audit', budget: 65000, spent: 61000, forecast: 72000, status: 'OVER_BUDGET' },
];

export function healthStatusClass(status: HealthStatus): string {
  const map: Record<HealthStatus, string> = {
    HEALTHY: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300',
    WARNING: 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300',
    CRITICAL: 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300',
  };
  return map[status];
}

export function loadStatusClass(status: EmployeeLoadItem['status']): string {
  const map: Record<EmployeeLoadItem['status'], string> = {
    NORMAL: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300',
    HIGH: 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300',
    OVERLOADED: 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300',
  };
  return map[status];
}

export function budgetStatusClass(status: BudgetStatusItem['status']): string {
  const map: Record<BudgetStatusItem['status'], string> = {
    ON_TRACK: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300',
    WARNING: 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300',
    OVER_BUDGET: 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300',
  };
  return map[status];
}
