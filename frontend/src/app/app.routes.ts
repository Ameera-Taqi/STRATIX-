import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/guards/auth.guard';
import { moduleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'auth/login' },
  {
    path: 'auth/login',
    loadComponent: () => import('./pages/auth/login.component').then((m) => m.LoginComponent),
    canActivate: [guestGuard],
  },
  {
    path: 'auth/register',
    loadComponent: () => import('./pages/auth/register.component').then((m) => m.RegisterComponent),
    canActivate: [guestGuard],
  },
  {
    path: 'auth/forgot-password',
    loadComponent: () =>
      import('./pages/auth/forgot-password.component').then((m) => m.ForgotPasswordComponent),
    canActivate: [guestGuard],
  },
  {
    path: 'auth/reset-password',
    loadComponent: () =>
      import('./pages/auth/reset-password.component').then((m) => m.ResetPasswordComponent),
    canActivate: [guestGuard],
  },
  {
    path: 'forbidden',
    loadComponent: () =>
      import('./pages/errors/forbidden.component').then((m) => m.ForbiddenComponent),
    canActivate: [authGuard],
  },
  {
    path: '',
    loadComponent: () =>
      import('./layout/dashboard-layout/dashboard-layout.component').then(
        (m) => m.DashboardLayoutComponent,
      ),
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./pages/dashboard/dashboard.component').then((m) => m.DashboardComponent),
        canActivate: [moduleGuard('DASHBOARD')],
      },
      {
        path: 'projects',
        loadComponent: () =>
          import('./pages/projects/projects.component').then((m) => m.ProjectsComponent),
        canActivate: [moduleGuard('PROJECTS')],
      },
      {
        path: 'projects/:id',
        loadComponent: () =>
          import('./pages/projects/project-detail.component').then((m) => m.ProjectDetailComponent),
        canActivate: [moduleGuard('PROJECTS')],
      },
      { path: 'timeline', pathMatch: 'full', redirectTo: 'projects' },
      { path: 'tasks', pathMatch: 'full', redirectTo: 'projects' },
      {
        path: 'tasks/:id',
        loadComponent: () =>
          import('./pages/tasks/task-detail.component').then((m) => m.TaskDetailComponent),
        canActivate: [moduleGuard('PROJECTS')],
      },
      {
        path: 'team',
        loadComponent: () => import('./pages/team/team.component').then((m) => m.TeamComponent),
        canActivate: [moduleGuard('EMPLOYEES')],
      },
      {
        path: 'team/:id',
        loadComponent: () =>
          import('./pages/team/employee-detail.component').then((m) => m.EmployeeDetailComponent),
        canActivate: [moduleGuard('EMPLOYEES')],
      },
      {
        path: 'performance',
        loadComponent: () =>
          import('./pages/performance/performance.component').then((m) => m.PerformanceComponent),
        canActivate: [moduleGuard('PERFORMANCE')],
      },
      {
        path: 'reports',
        loadComponent: () =>
          import('./pages/reports/reports.component').then((m) => m.ReportsComponent),
        canActivate: [moduleGuard('REPORTS')],
      },
      {
        path: 'risks',
        loadComponent: () => import('./pages/risks/risks.component').then((m) => m.RisksComponent),
        canActivate: [moduleGuard('RISKS')],
      },
      {
        path: 'risks/:id',
        loadComponent: () =>
          import('./pages/risks/risk-detail.component').then((m) => m.RiskDetailComponent),
        canActivate: [moduleGuard('RISKS')],
      },
      {
        path: 'notifications',
        loadComponent: () =>
          import('./pages/notifications/notifications.component').then(
            (m) => m.NotificationsComponent,
          ),
        canActivate: [moduleGuard('NOTIFICATIONS')],
      },
      {
        path: 'audit',
        loadComponent: () =>
          import('./pages/audit-log/audit-log.component').then((m) => m.AuditLogComponent),
        canActivate: [moduleGuard('AUDIT')],
      },
      {
        path: 'organizations',
        loadComponent: () =>
          import('./pages/organizations/organizations.component').then(
            (m) => m.OrganizationsComponent,
          ),
        canActivate: [moduleGuard('ORGANIZATIONS')],
      },
      {
        path: 'cms',
        loadComponent: () => import('./pages/cms/cms.component').then((m) => m.CmsComponent),
        canActivate: [moduleGuard('PLATFORM_CMS')],
      },
      {
        path: 'roles',
        loadComponent: () => import('./pages/roles/roles.component').then((m) => m.RolesComponent),
        canActivate: [moduleGuard('ROLES')],
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('./pages/settings/settings.component').then((m) => m.SettingsComponent),
        canActivate: [moduleGuard('SETTINGS')],
      },
    ],
  },
  {
    path: '**',
    loadComponent: () =>
      import('./pages/errors/not-found.component').then((m) => m.NotFoundComponent),
  },
];
