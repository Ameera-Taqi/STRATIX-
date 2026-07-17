export type MilestoneStatus = 'PENDING' | 'IN_PROGRESS' | 'COMPLETED' | 'DELAYED';

export interface MilestoneResponse {
  id: number;
  projectId: number;
  projectName: string;
  title: string;
  dueDate: string;
  completedDate: string | null;
  status: MilestoneStatus | string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateMilestoneRequest {
  projectId: number;
  title: string;
  dueDate: string;
  completedDate?: string | null;
  status?: MilestoneStatus | null;
}

export interface UpdateMilestoneRequest {
  projectId: number;
  title: string;
  dueDate: string;
  completedDate?: string | null;
  status: MilestoneStatus;
}
