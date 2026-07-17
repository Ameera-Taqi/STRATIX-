import { UserRole } from '../models/user.model';

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
  | 'SETTINGS';

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
    labelKey: 'nav.timeline',
    path: '/timeline',
    sidebar: true,
    rolesRead: ALL_ROLES,
    rolesWrite: PM_AND_ABOVE,
  },
  {
    id: 5,
    code: 'TASKS',
    labelKey: 'nav.tasks',
    path: '/tasks',
    sidebar: true,
    rolesRead: ALL_ROLES,
    rolesWrite: [...LEADERS_AND_ABOVE, 'EMPLOYEE'],
  },
  {
    id: 6,
    code: 'EMPLOYEES',
    labelKey: 'nav.employees',
    path: '/team',
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
] as const;

export const SIDEBAR_MODULES = STRATIX_MODULES.filter((m) => m.sidebar && m.path);

export function canAccessModule(
  module: StratixModule,
  role: UserRole,
  mode: 'read' | 'write' = 'read'
): boolean {
  // Platform and organization administrators have full access to every module.
  if (role === 'SUPER_ADMIN' || role === 'ORG_ADMIN') return true;
  const list = mode === 'write' ? module.rolesWrite : module.rolesRead;
  return list.includes(role);
}
