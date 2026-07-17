export type UserRole =
  | 'SUPER_ADMIN'
  | 'ORG_ADMIN'
  | 'ADMIN'
  | 'PROJECT_MANAGER'
  | 'TEAM_LEADER'
  | 'EMPLOYEE'
  | 'EXECUTIVE_VIEWER';

export type UserStatus = 'ACTIVE' | 'INACTIVE' | 'SUSPENDED';

export interface User {
  id: number;
  name: string;
  email: string;
  role: UserRole;
  jobTitle: string | null;
  status: UserStatus;
  departmentId: number | null;
  departmentName: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface HealthResponse {
  status: string;
  application: string;
}
