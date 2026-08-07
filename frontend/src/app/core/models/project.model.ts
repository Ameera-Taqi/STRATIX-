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

export interface CreateProjectRequest {
  name: string;
  description?: string | null;
  departmentId: number | null;
  projectManagerId: number | null;
  startDate: string | null;
  endDate: string | null;
  status: string;
  priority: string;
  progress?: number;
}

export interface UpdateProjectRequest {
  name: string;
  description?: string | null;
  departmentId: number | null;
  projectManagerId: number | null;
  startDate: string | null;
  endDate: string | null;
  status: string;
  priority: string;
  progress?: number;
}
