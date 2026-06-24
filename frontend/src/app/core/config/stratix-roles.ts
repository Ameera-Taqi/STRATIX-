import { UserRole } from '../models/user.model';

/** Official Stratix roles — see docs/OFFICIAL_SPEC.md */
export interface StratixRole {
  id: number;
  code: UserRole;
  labelKey: string;
}

export const STRATIX_ROLES: readonly StratixRole[] = [
  { id: 1, code: 'ADMIN', labelKey: 'role.admin' },
  { id: 2, code: 'PROJECT_MANAGER', labelKey: 'role.projectManager' },
  { id: 3, code: 'TEAM_LEADER', labelKey: 'role.teamLeader' },
  { id: 4, code: 'EMPLOYEE', labelKey: 'role.employee' },
  { id: 5, code: 'EXECUTIVE_VIEWER', labelKey: 'role.executiveViewer' },
] as const;

export function roleLabelKey(code: UserRole): string {
  return STRATIX_ROLES.find((r) => r.code === code)?.labelKey ?? code;
}
