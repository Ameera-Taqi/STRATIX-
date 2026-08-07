import { UserRole } from '../models/user.model';

/** Dashboard widget keys — each role gets a curated subset. */
export type DashboardWidget =
  | 'projects'
  | 'healthOverview'
  | 'criticalRisks'
  | 'teamPerformance'
  | 'subscriptionUsage'
  | 'myProjects'
  | 'tasksAwaitingReview'
  | 'delayedTasks'
  | 'teamWorkload'
  | 'projectHealth'
  | 'teamTasks'
  | 'blockedTasks'
  | 'reviewQueue'
  | 'featureProgress'
  | 'myTasks'
  | 'dueToday'
  | 'overdue'
  | 'inReview'
  | 'myKpi'
  | 'notifications'
  | 'projectPortfolio'
  | 'healthTrends'
  | 'criticalProjects'
  | 'riskExposure'
  | 'performance';

const ADMIN_WIDGETS: DashboardWidget[] = [
  'projects',
  'healthOverview',
  'criticalRisks',
  'teamPerformance',
  'subscriptionUsage',
];

const PM_WIDGETS: DashboardWidget[] = [
  'myProjects',
  'tasksAwaitingReview',
  'delayedTasks',
  'criticalRisks',
  'teamWorkload',
  'projectHealth',
];

const TL_WIDGETS: DashboardWidget[] = [
  'teamTasks',
  'blockedTasks',
  'reviewQueue',
  'featureProgress',
];

const EMPLOYEE_WIDGETS: DashboardWidget[] = [
  'myTasks',
  'dueToday',
  'overdue',
  'inReview',
  'myKpi',
  'notifications',
];

const EXEC_WIDGETS: DashboardWidget[] = [
  'projectPortfolio',
  'healthTrends',
  'criticalProjects',
  'riskExposure',
  'performance',
];

const BY_ROLE: Record<UserRole, DashboardWidget[]> = {
  SUPER_ADMIN: ADMIN_WIDGETS,
  ORG_ADMIN: ADMIN_WIDGETS,
  ADMIN: ADMIN_WIDGETS,
  PROJECT_MANAGER: PM_WIDGETS,
  TEAM_LEADER: TL_WIDGETS,
  EMPLOYEE: EMPLOYEE_WIDGETS,
  EXECUTIVE_VIEWER: EXEC_WIDGETS,
};

export function dashboardWidgetsFor(role: UserRole | null | undefined): DashboardWidget[] {
  if (!role) return EXEC_WIDGETS;
  return BY_ROLE[role] ?? EXEC_WIDGETS;
}

export function dashboardHasWidget(
  role: UserRole | null | undefined,
  widget: DashboardWidget,
): boolean {
  return dashboardWidgetsFor(role).includes(widget);
}

export function dashboardTitleKey(role: UserRole | null | undefined): string {
  switch (role) {
    case 'ORG_ADMIN':
    case 'ADMIN':
    case 'SUPER_ADMIN':
      return 'dashboard.roleTitle.admin';
    case 'PROJECT_MANAGER':
      return 'dashboard.roleTitle.pm';
    case 'TEAM_LEADER':
      return 'dashboard.roleTitle.tl';
    case 'EMPLOYEE':
      return 'dashboard.roleTitle.employee';
    case 'EXECUTIVE_VIEWER':
      return 'dashboard.roleTitle.executive';
    default:
      return 'page.dashboard';
  }
}

export function dashboardHintKey(role: UserRole | null | undefined): string {
  switch (role) {
    case 'ORG_ADMIN':
    case 'ADMIN':
    case 'SUPER_ADMIN':
      return 'dashboard.roleHint.admin';
    case 'PROJECT_MANAGER':
      return 'dashboard.roleHint.pm';
    case 'TEAM_LEADER':
      return 'dashboard.roleHint.tl';
    case 'EMPLOYEE':
      return 'dashboard.roleHint.employee';
    case 'EXECUTIVE_VIEWER':
      return 'dashboard.roleHint.executive';
    default:
      return 'dashboard.chartOverviewHint';
  }
}
