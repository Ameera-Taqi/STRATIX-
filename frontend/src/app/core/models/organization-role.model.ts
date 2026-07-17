export interface OrganizationRoleRow {
  id: number;
  code: string;
  name: string;
  description: string | null;
  baseRole: string;
  isSystem: boolean;
  createdAt: string;
}

export interface CreateOrganizationRoleRequest {
  name: string;
  code?: string | null;
  description?: string | null;
  baseRole: string;
}

export interface UpdateOrganizationRoleRequest {
  name: string;
  description?: string | null;
  baseRole: string;
}
