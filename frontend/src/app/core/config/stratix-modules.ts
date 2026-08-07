import { UserRole } from '../models/user.model';
import type { CmsPermissionMap } from '../services/cms-permissions.store';

/** Official Stratix modules — see docs/OFFICIAL_SPEC.md */
export type SystemModuleCode =
  | 'AUTH'
  | 'DASHBOARD'
  | 'PROJECTS'
  | 'STAGES'
  | 'TASKS'
  | 'EMPLOYEES'
  | 'PERFORMANCE'
  | 'REPORTS'
  | 'NOTIFICATIONS'
  | 'AUDIT'
  | 'RISKS'
  | 'SETTINGS'
  | 'ORGANIZATIONS'
  | 'PLATFORM_CMS'
  | 'ROLES'
  | 'DEPARTMENTS'
  | 'BRANDING';

export interface StratixModule {
  id: number;
  code: SystemModuleCode;
  labelKey: string;
  path?: string;
  sidebar: boolean;
  rolesRead: UserRole[];
  rolesWrite: UserRole[];
}

const ALL_ROLES: UserRole[] = [
  'ADMIN',
  'PROJECT_MANAGER',
  'TEAM_LEADER',
  'EMPLOYEE',
  'EXECUTIVE_VIEWER',
];

const PM_AND_ABOVE: UserRole[] = ['ADMIN', 'PROJECT_MANAGER'];
const LEADERS_AND_ABOVE: UserRole[] = ['ADMIN', 'PROJECT_MANAGER', 'TEAM_LEADER'];
const READ_MOST: UserRole[] = ['ADMIN', 'PROJECT_MANAGER', 'TEAM_LEADER', 'EXECUTIVE_VIEWER'];

const COMPANY_ADMIN_ROLES: UserRole[] = ['ORG_ADMIN', 'ADMIN'];

/** Fixed list — order matches official spec #1–#10 */
export const STRATIX_MODULES: readonly StratixModule[] = [
  {
    id: 1,
    code: 'AUTH',
    labelKey: 'module.auth',
    path: '/auth/login',
    sidebar: false,
    rolesRead: ALL_ROLES,
    rolesWrite: [],
  },
  {
    id: 2,
    code: 'DASHBOARD',
    labelKey: 'nav.dashboard',
    path: '/dashboard',
    sidebar: true,
    rolesRead: ALL_ROLES,
    rolesWrite: ['ADMIN'],
  },
  {
    id: 3,
    code: 'PROJECTS',
    labelKey: 'nav.projects',
    path: '/projects',
    sidebar: true,
    rolesRead: ALL_ROLES,
    rolesWrite: PM_AND_ABOVE,
  },
  {
    id: 4,
    code: 'STAGES',
    labelKey: 'module.stages',
    path: '/timeline',
    sidebar: false,
    rolesRead: ALL_ROLES,
    rolesWrite: PM_AND_ABOVE,
  },
  {
    id: 5,
    code: 'TASKS',
    labelKey: 'nav.tasks',
    path: '/tasks',
    sidebar: false,
    rolesRead: ALL_ROLES,
    rolesWrite: [...LEADERS_AND_ABOVE, 'EMPLOYEE'],
  },
  {
    id: 6,
    code: 'EMPLOYEES',
    labelKey: 'nav.team',
    path: '/team',
    sidebar: true,
    rolesRead: READ_MOST,
    rolesWrite: ['ADMIN'],
  },
  {
    id: 17,
    code: 'DEPARTMENTS',
    labelKey: 'nav.departments',
    path: '/departments',
    sidebar: true,
    rolesRead: READ_MOST,
    rolesWrite: ['ADMIN'],
  },
  {
    id: 7,
    code: 'PERFORMANCE',
    labelKey: 'nav.performance',
    path: '/performance',
    sidebar: true,
    rolesRead: READ_MOST,
    rolesWrite: LEADERS_AND_ABOVE,
  },
  {
    id: 8,
    code: 'REPORTS',
    labelKey: 'nav.reports',
    path: '/reports',
    sidebar: true,
    rolesRead: READ_MOST,
    rolesWrite: LEADERS_AND_ABOVE,
  },
  {
    id: 9,
    code: 'NOTIFICATIONS',
    labelKey: 'nav.notifications',
    path: '/notifications',
    sidebar: true,
    rolesRead: ALL_ROLES,
    rolesWrite: ALL_ROLES,
  },
  {
    id: 11,
    code: 'RISKS',
    labelKey: 'nav.risks',
    path: '/risks',
    sidebar: true,
    rolesRead: ALL_ROLES,
    rolesWrite: PM_AND_ABOVE,
  },
  {
    id: 12,
    code: 'AUDIT',
    labelKey: 'nav.audit',
    path: '/audit',
    sidebar: true,
    rolesRead: LEADERS_AND_ABOVE,
    rolesWrite: ['ADMIN'],
  },
  {
    id: 10,
    code: 'SETTINGS',
    labelKey: 'nav.settings',
    path: '/settings',
    sidebar: true,
    rolesRead: ALL_ROLES,
    rolesWrite: ['ADMIN'],
  },
  {
    id: 15,
    code: 'ROLES',
    labelKey: 'nav.roles',
    path: '/roles',
    sidebar: true,
    rolesRead: READ_MOST,
    rolesWrite: ['ADMIN'],
  },
  {
    id: 16,
    code: 'BRANDING',
    labelKey: 'nav.branding',
    sidebar: false,
    rolesRead: ['ADMIN'],
    rolesWrite: ['ADMIN'],
  },
  {
    id: 13,
    code: 'ORGANIZATIONS',
    labelKey: 'nav.organizations',
    path: '/organizations',
    sidebar: true,
    rolesRead: ['SUPER_ADMIN'],
    rolesWrite: ['SUPER_ADMIN'],
  },
  {
    id: 14,
    code: 'PLATFORM_CMS',
    labelKey: 'nav.cms',
    path: '/cms',
    sidebar: true,
    rolesRead: ['SUPER_ADMIN'],
    rolesWrite: ['SUPER_ADMIN'],
  },
] as const;

export const SIDEBAR_MODULES = STRATIX_MODULES.filter((m) => m.sidebar && m.path);

export function canAccessModule(
  module: StratixModule,
  role: UserRole,
  mode: 'read' | 'write' = 'read',
  cmsMap?: CmsPermissionMap | null,
): boolean {
  // Platform-only surfaces.
  if (module.code === 'ORGANIZATIONS' || module.code === 'PLATFORM_CMS') {
    return role === 'SUPER_ADMIN';
  }

  if (role === 'SUPER_ADMIN') return true;

  // Company admins: gated by Super Admin CMS settings. Fail closed when CMS
  // has not loaded or the module has no permission row.
  if (COMPANY_ADMIN_ROLES.includes(role)) {
    const cms =
      cmsMap?.[module.code] ??
      // New DEPARTMENTS module: reuse ROLES grant until CMS defaults are seeded.
      (module.code === 'DEPARTMENTS' ? cmsMap?.['ROLES'] : undefined);
    if (!cms) return false;
    if (mode === 'read') return cms.visible;
    return cms.visible && cms.writable;
  }

  const list = mode === 'write' ? module.rolesWrite : module.rolesRead;
  return list.includes(role);
}
