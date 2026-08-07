export interface StageRow {
  id: number;
  name: string;
  description?: string | null;
  startDate: string;
  endDate: string;
  progress: number;
  status: string;
  orderNumber: number;
}

export interface CreateStageRequest {
  name: string;
  description?: string | null;
  startDate?: string | null;
  endDate?: string | null;
  status?: string;
  orderNumber?: number;
}

export interface UpdateStageRequest {
  name: string;
  description?: string | null;
  startDate?: string | null;
  endDate?: string | null;
  status?: string;
  orderNumber?: number;
  progress?: number;
}
