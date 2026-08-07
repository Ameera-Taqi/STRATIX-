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

/** Lightweight directory entry for assignee/manager pickers (no admin timestamps). */
export interface UserDirectoryItem {
  id: number;
  name: string;
  email: string;
  role: UserRole;
  jobTitle: string | null;
  status: UserStatus;
  departmentId: number | null;
  departmentName: string | null;
}

export interface HealthResponse {
  status: string;
  application: string;
}

export interface CreateUserRequest {
  name: string;
  email: string;
  password: string;
  role: UserRole;
  jobTitle: string | null;
  status: UserStatus;
  departmentId: number | null;
}

export interface UpdateUserRequest {
  name: string;
  email: string;
  role: UserRole;
  jobTitle: string | null;
  status: UserStatus;
  departmentId: number | null;
}
