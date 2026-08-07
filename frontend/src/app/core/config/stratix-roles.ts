import { UserRole } from '../models/user.model';

/** Official Stratix roles — see docs/OFFICIAL_SPEC.md */
export interface StratixRole {
  id: number;
  code: UserRole;
  labelKey: string;
  descriptionKey: string;
}

export const STRATIX_ROLES: readonly StratixRole[] = [
  { id: 6, code: 'SUPER_ADMIN', labelKey: 'role.superAdmin', descriptionKey: 'role.desc.superAdmin' },
  { id: 7, code: 'ORG_ADMIN', labelKey: 'role.orgAdmin', descriptionKey: 'role.desc.orgAdmin' },
  { id: 1, code: 'ADMIN', labelKey: 'role.admin', descriptionKey: 'role.desc.admin' },
  { id: 2, code: 'PROJECT_MANAGER', labelKey: 'role.projectManager', descriptionKey: 'role.desc.projectManager' },
  { id: 3, code: 'TEAM_LEADER', labelKey: 'role.teamLeader', descriptionKey: 'role.desc.teamLeader' },
  { id: 4, code: 'EMPLOYEE', labelKey: 'role.employee', descriptionKey: 'role.desc.employee' },
  { id: 5, code: 'EXECUTIVE_VIEWER', labelKey: 'role.executiveViewer', descriptionKey: 'role.desc.executiveViewer' },
] as const;

const TENANT_ROLES: UserRole[] = [
  'ADMIN',
  'PROJECT_MANAGER',
  'TEAM_LEADER',
  'EMPLOYEE',
  'EXECUTIVE_VIEWER',
];

export function roleLabelKey(code: string): string {
  return STRATIX_ROLES.find((r) => r.code === code)?.labelKey ?? code;
}

export function roleDescriptionKey(code: string): string {
  return STRATIX_ROLES.find((r) => r.code === code)?.descriptionKey ?? code;
}

/**
 * Read-only data access label for a role — mirrors backend RoleDataScope.
 * Not editable in UI; helps admins understand why someone cannot see a project.
 */
export type RoleDataScopeKind =
  | 'ORG_WIDE'
  | 'MANAGED_PROJECTS'
  | 'TEAM_PROJECTS'
  | 'ASSIGNED_TASKS';

export function roleDataScopeKind(code: string): RoleDataScopeKind {
  switch (code) {
    case 'PROJECT_MANAGER':
      return 'MANAGED_PROJECTS';
    case 'TEAM_LEADER':
      return 'TEAM_PROJECTS';
    case 'EMPLOYEE':
      return 'ASSIGNED_TASKS';
    case 'SUPER_ADMIN':
    case 'ORG_ADMIN':
    case 'ADMIN':
    case 'EXECUTIVE_VIEWER':
    default:
      return 'ORG_WIDE';
  }
}

export function roleDataScopeLabelKey(code: string): string {
  switch (roleDataScopeKind(code)) {
    case 'MANAGED_PROJECTS':
      return 'role.scope.managedProjects';
    case 'TEAM_PROJECTS':
      return 'role.scope.teamProjects';
    case 'ASSIGNED_TASKS':
      return 'role.scope.assignedTasks';
    case 'ORG_WIDE':
    default:
      return 'role.scope.orgWide';
  }
}

/** Roles the caller may assign — mirrors backend UserService.ValidateRoleAssignment. */
export function assignableRolesFor(caller: UserRole): UserRole[] {
  if (caller === 'SUPER_ADMIN') return ['SUPER_ADMIN', 'ORG_ADMIN', ...TENANT_ROLES];
  if (caller === 'ORG_ADMIN') return ['ORG_ADMIN', ...TENANT_ROLES];
  if (caller === 'ADMIN') return [...TENANT_ROLES];
  if (caller === 'PROJECT_MANAGER') {
    return ['PROJECT_MANAGER', 'TEAM_LEADER', 'EMPLOYEE', 'EXECUTIVE_VIEWER'];
  }
  return [];
}

/** Roles shown on the tenant Roles page (excludes platform Super Admin unless caller is SA). */
export function visibleRolesFor(caller: UserRole): readonly StratixRole[] {
  if (caller === 'SUPER_ADMIN') return STRATIX_ROLES;
  return STRATIX_ROLES.filter((r) => r.code !== 'SUPER_ADMIN');
}
