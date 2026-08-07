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
  startDate?: string;
  dueDate: string;
  status: 'TODO' | 'IN_PROGRESS' | 'REVIEW' | 'DONE' | 'BLOCKED';
  description?: string;
  estimatedHours?: number;
  blockedReason?: string | null;
  reopenReason?: string | null;
  reviewReason?: string | null;
  submittedForReviewAt?: string | null;
  submittedForReviewById?: number | null;
  submittedForReviewBy?: string | null;
}

export interface CreateTaskRequest {
  projectId: number;
  stageId?: number | null;
  title: string;
  description?: string | null;
  status?: string;
  priority?: string;
  assigneeId?: number | null;
  startDate?: string | null;
  dueDate?: string | null;
  estimatedHours?: number | null;
}

export interface UpdateTaskRequest {
  projectId?: number;
  stageId?: number | null;
  title?: string;
  description?: string | null;
  status?: string;
  priority?: string;
  assigneeId?: number | null;
  startDate?: string | null;
  dueDate?: string | null;
  estimatedHours?: number | null;
}
