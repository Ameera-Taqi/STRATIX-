export interface ProjectRow {
  id: number;
  name: string;
  department: string;
  manager: string;
  managerId?: number | null;
  startDate: string;
  endDate: string;
  progress: number;
  status: string;
  priority?: 'LOW' | 'MEDIUM' | 'HIGH' | 'URGENT';
  owner: string;
  deadline: string;
}

export interface TaskCard {
  id: number;
  projectId: number;
  projectName: string;
  stageId: number | null;
  stageName: string | null;
  title: string;
  assignee: string;
  assigneeId: number | null;
  priority: 'LOW' | 'MEDIUM' | 'HIGH' | 'URGENT';
  dueDate: string;
  status: 'TODO' | 'IN_PROGRESS' | 'REVIEW' | 'DONE';
  description?: string;
}

export interface StageRow {
  id: number;
  name: string;
  startDate: string;
  endDate: string;
  progress: number;
  status: string;
  orderNumber: number;
}

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

export const MOCK_PROJECTS: ProjectRow[] = [
  {
    id: 1,
    name: 'ERP Rollout',
    department: 'IT',
    manager: 'Sara Ali',
    startDate: '2026-01-10',
    endDate: '2026-07-15',
    progress: 72,
    status: 'Active',
    priority: 'HIGH',
    owner: 'Sara Ali',
    deadline: '2026-07-15',
  },
  {
    id: 2,
    name: 'Mobile App v2',
    department: 'Product',
    manager: 'Omar Hassan',
    startDate: '2026-02-01',
    endDate: '2026-08-01',
    progress: 45,
    status: 'Active',
    priority: 'MEDIUM',
    owner: 'Omar Hassan',
    deadline: '2026-08-01',
  },
  {
    id: 3,
    name: 'Data Migration',
    department: 'Operations',
    manager: 'Lina Noor',
    startDate: '2025-11-01',
    endDate: '2026-06-20',
    progress: 90,
    status: 'Review',
    priority: 'URGENT',
    owner: 'Lina Noor',
    deadline: '2026-06-20',
  },
  {
    id: 4,
    name: 'Security Audit',
    department: 'IT',
    manager: 'Khalid Fahad',
    startDate: '2026-03-01',
    endDate: '2026-05-30',
    progress: 30,
    status: 'Delayed',
    priority: 'HIGH',
    owner: 'Khalid Fahad',
    deadline: '2026-05-30',
  },
];

export const MOCK_TASKS: TaskCard[] = [
  {
    id: 1,
    projectId: 1,
    projectName: 'ERP Rollout',
    stageId: 2,
    stageName: 'Implementation',
    title: 'API integration',
    assignee: 'Sara Ali',
    assigneeId: 1,
    priority: 'HIGH',
    dueDate: '2026-06-10',
    status: 'TODO',
    description: 'Connect payroll module to core ERP.',
  },
  {
    id: 2,
    projectId: 2,
    projectName: 'Mobile App v2',
    stageId: null,
    stageName: null,
    title: 'UI mockups',
    assignee: 'Omar Hassan',
    assigneeId: 2,
    priority: 'MEDIUM',
    dueDate: '2026-06-05',
    status: 'IN_PROGRESS',
  },
  {
    id: 3,
    projectId: 3,
    projectName: 'Data Migration',
    stageId: null,
    stageName: null,
    title: 'Schema validation',
    assignee: 'Lina Noor',
    assigneeId: 3,
    priority: 'URGENT',
    dueDate: '2026-06-08',
    status: 'REVIEW',
  },
  {
    id: 4,
    projectId: 1,
    projectName: 'ERP Rollout',
    stageId: 2,
    stageName: 'Implementation',
    title: 'Deploy staging',
    assignee: 'Sara Ali',
    assigneeId: 1,
    priority: 'LOW',
    dueDate: '2026-06-12',
    status: 'DONE',
  },
  {
    id: 5,
    projectId: 4,
    projectName: 'Security Audit',
    stageId: null,
    stageName: null,
    title: 'Pen test report',
    assignee: 'Khalid Fahad',
    assigneeId: 4,
    priority: 'HIGH',
    dueDate: '2026-06-15',
    status: 'IN_PROGRESS',
  },
];

export const MOCK_STAGES: StageRow[] = [
  { id: 1, name: 'Discovery', startDate: '2026-01-10', endDate: '2026-02-28', progress: 100, status: 'Done', orderNumber: 1 },
  { id: 2, name: 'Implementation', startDate: '2026-03-01', endDate: '2026-06-30', progress: 65, status: 'Active', orderNumber: 2 },
  { id: 3, name: 'UAT & Go-live', startDate: '2026-07-01', endDate: '2026-07-15', progress: 10, status: 'Planned', orderNumber: 3 },
];

export const MOCK_EMPLOYEES: EmployeeRow[] = [
  { id: 1, name: 'Sara Ali', role: 'Project Manager', department: 'IT', projects: 3, completedTasks: 42, status: 'Active', tasksCompleted: 42, delayedTasks: 2, onTimePct: 95, kpiScore: 92 },
  { id: 2, name: 'Omar Hassan', role: 'Team Leader', department: 'Product', projects: 2, completedTasks: 38, status: 'Active', tasksCompleted: 38, delayedTasks: 4, onTimePct: 89, kpiScore: 88 },
  { id: 3, name: 'Lina Noor', role: 'Employee', department: 'Operations', projects: 1, completedTasks: 28, status: 'Active', tasksCompleted: 28, delayedTasks: 1, onTimePct: 96, kpiScore: 94 },
  { id: 4, name: 'Khalid Fahad', role: 'Project Manager', department: 'Finance', projects: 1, completedTasks: 0, status: 'Active', tasksCompleted: 0, delayedTasks: 0, onTimePct: 100, kpiScore: 0 },
];

export interface RiskRow {
  id: number;
  title: string;
  description: string;
  impact: 'LOW' | 'MEDIUM' | 'HIGH';
  probability: 'LOW' | 'MEDIUM' | 'HIGH';
  riskLevel: 'LOW' | 'MEDIUM' | 'HIGH' | 'CRITICAL';
  mitigationPlan: string;
  status: 'OPEN' | 'MITIGATING' | 'CLOSED';
  projectId: number;
  projectName: string;
  ownerId: number;
  ownerName: string;
  createdAt: string;
  updatedAt: string;
}

export const MOCK_RISKS: RiskRow[] = [
  {
    id: 1,
    title: 'Vendor API instability',
    description: 'Third-party payroll API has intermittent outages',
    impact: 'HIGH',
    probability: 'HIGH',
    riskLevel: 'CRITICAL',
    mitigationPlan: 'Establish fallback batch sync and SLA monitoring',
    status: 'OPEN',
    projectId: 1,
    projectName: 'ERP Rollout',
    ownerId: 1,
    ownerName: 'Sara Ali',
    createdAt: '2026-05-01T10:00:00Z',
    updatedAt: '2026-06-10T14:30:00Z',
  },
  {
    id: 2,
    title: 'Data migration errors',
    description: 'Legacy data may not map cleanly to new schema',
    impact: 'HIGH',
    probability: 'MEDIUM',
    riskLevel: 'HIGH',
    mitigationPlan: 'Run staged migration with validation scripts',
    status: 'MITIGATING',
    projectId: 1,
    projectName: 'ERP Rollout',
    ownerId: 1,
    ownerName: 'Sara Ali',
    createdAt: '2026-05-03T09:00:00Z',
    updatedAt: '2026-06-08T11:00:00Z',
  },
  {
    id: 3,
    title: 'Scope creep',
    description: 'Stakeholders requesting additional features mid-sprint',
    impact: 'MEDIUM',
    probability: 'MEDIUM',
    riskLevel: 'MEDIUM',
    mitigationPlan: 'Change control board review for all new requests',
    status: 'OPEN',
    projectId: 2,
    projectName: 'Mobile App v2',
    ownerId: 1,
    ownerName: 'Sara Ali',
    createdAt: '2026-05-10T08:00:00Z',
    updatedAt: '2026-06-01T16:00:00Z',
  },
  {
    id: 4,
    title: 'App store rejection',
    description: 'Policy compliance issues during submission',
    impact: 'MEDIUM',
    probability: 'LOW',
    riskLevel: 'LOW',
    mitigationPlan: 'Pre-submission compliance checklist',
    status: 'CLOSED',
    projectId: 2,
    projectName: 'Mobile App v2',
    ownerId: 2,
    ownerName: 'Omar Hassan',
    createdAt: '2026-04-15T12:00:00Z',
    updatedAt: '2026-05-20T09:00:00Z',
  },
  {
    id: 5,
    title: 'Training adoption lag',
    description: 'End users slow to adopt new workflows',
    impact: 'LOW',
    probability: 'LOW',
    riskLevel: 'LOW',
    mitigationPlan: 'Phased training program with champions',
    status: 'CLOSED',
    projectId: 1,
    projectName: 'ERP Rollout',
    ownerId: 1,
    ownerName: 'Sara Ali',
    createdAt: '2026-03-01T10:00:00Z',
    updatedAt: '2026-05-01T10:00:00Z',
  },
];
