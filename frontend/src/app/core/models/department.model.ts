export interface DepartmentRow {
  id: number;
  name: string;
  description: string | null;
}

export interface CreateDepartmentRequest {
  name: string;
  description: string | null;
}

export interface UpdateDepartmentRequest {
  name: string;
  description: string | null;
}
