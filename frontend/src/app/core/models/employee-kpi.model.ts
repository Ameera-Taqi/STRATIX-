export interface EmployeeKpiResponse {
  id: number;
  userId: number;
  userName: string;
  period: string;
  tasksCompleted: number;
  tasksOnTime: number;
  score: number;
  notes: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateEmployeeKpiRequest {
  userId: number;
  period: string;
  tasksCompleted: number;
  tasksOnTime: number;
  score: number;
  notes?: string | null;
}

export interface UpdateEmployeeKpiRequest {
  period: string;
  tasksCompleted: number;
  tasksOnTime: number;
  score: number;
  notes?: string | null;
}
