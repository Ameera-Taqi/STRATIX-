export type ChangeRequestStatus = 'PENDING' | 'APPROVED' | 'REJECTED' | 'IMPLEMENTED';
export type ChangeRequestPriority = 'LOW' | 'MEDIUM' | 'HIGH' | 'URGENT';

export interface ChangeRequestResponse {
  id: number;
  projectId: number;
  projectName: string;
  title: string;
  description: string | null;
  status: ChangeRequestStatus | string;
  priority: ChangeRequestPriority | string;
  requestedById: number;
  requestedByName: string;
  reviewedById: number | null;
  reviewedByName: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateChangeRequestRequest {
  projectId: number;
  title: string;
  description?: string | null;
  priority?: ChangeRequestPriority | null;
  requestedById: number;
}

export interface UpdateChangeRequestRequest {
  title: string;
  description?: string | null;
  status: ChangeRequestStatus;
  priority: ChangeRequestPriority;
  reviewedById?: number | null;
}
