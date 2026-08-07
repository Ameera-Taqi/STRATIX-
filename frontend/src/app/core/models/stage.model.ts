export interface StageRow {
  id: number;
  name: string;
  startDate: string;
  endDate: string;
  progress: number;
  status: string;
  orderNumber: number;
}

export interface CreateStageRequest {
  name: string;
  startDate?: string | null;
  endDate?: string | null;
  status?: string;
  orderNumber?: number;
  progress?: number;
}

export interface UpdateStageRequest {
  name: string;
  startDate?: string | null;
  endDate?: string | null;
  status?: string;
  orderNumber?: number;
  progress?: number;
}
