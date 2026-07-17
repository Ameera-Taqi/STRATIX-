import { Routes } from '@angular/router';
import { DashboardLayoutComponent } from './layout/dashboard-layout/dashboard-layout.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { ProjectsComponent } from './pages/projects/projects.component';
import { ProjectDetailComponent } from './pages/projects/project-detail.component';
import { TaskDetailComponent } from './pages/tasks/task-detail.component';
import { TeamComponent } from './pages/team/team.component';
import { EmployeeDetailComponent } from './pages/team/employee-detail.component';
import { PerformanceComponent } from './pages/performance/performance.component';
import { ReportsComponent } from './pages/reports/reports.component';
import { SettingsComponent } from './pages/settings/settings.component';
import { NotificationsComponent } from './pages/notifications/notifications.component';
import { RisksComponent } from './pages/risks/risks.component';
import { RiskDetailComponent } from './pages/risks/risk-detail.component';
import { AuditLogComponent } from './pages/audit-log/audit-log.component';
import { OrganizationsComponent } from './pages/organizations/organizations.component';
import { CmsComponent } from './pages/cms/cms.component';
import { RolesComponent } from './pages/roles/roles.component';
import { LoginComponent } from './pages/auth/login.component';
import { RegisterComponent } from './pages/auth/register.component';
import { ForgotPasswordComponent } from './pages/auth/forgot-password.component';
import { ResetPasswordComponent } from './pages/auth/reset-password.component';
import { authGuard, guestGuard } from './core/guards/auth.guard';
import { moduleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'auth/login' },
  { path: 'auth/login', component: LoginComponent, canActivate: [guestGuard] },
  { path: 'auth/register', component: RegisterComponent, canActivate: [guestGuard] },
  { path: 'auth/forgot-password', component: ForgotPasswordComponent, canActivate: [guestGuard] },
  { path: 'auth/reset-password', component: ResetPasswordComponent, canActivate: [guestGuard] },
  {
    path: '',
    component: DashboardLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: DashboardComponent, canActivate: [moduleGuard('DASHBOARD')] },
      { path: 'projects', component: ProjectsComponent, canActivate: [moduleGuard('PROJECTS')] },
      { path: 'projects/:id', component: ProjectDetailComponent, canActivate: [moduleGuard('PROJECTS')] },
      { path: 'timeline', pathMatch: 'full', redirectTo: 'projects' },
      { path: 'tasks', pathMatch: 'full', redirectTo: 'projects' },
      { path: 'tasks/:id', component: TaskDetailComponent, canActivate: [moduleGuard('PROJECTS')] },
      { path: 'team', component: TeamComponent, canActivate: [moduleGuard('EMPLOYEES')] },
      { path: 'team/:id', component: EmployeeDetailComponent, canActivate: [moduleGuard('EMPLOYEES')] },
      { path: 'performance', component: PerformanceComponent, canActivate: [moduleGuard('PERFORMANCE')] },
      { path: 'reports', component: ReportsComponent, canActivate: [moduleGuard('REPORTS')] },
      { path: 'risks', component: RisksComponent, canActivate: [moduleGuard('RISKS')] },
      { path: 'risks/:id', component: RiskDetailComponent, canActivate: [moduleGuard('RISKS')] },
      { path: 'notifications', component: NotificationsComponent, canActivate: [moduleGuard('NOTIFICATIONS')] },
      { path: 'audit', component: AuditLogComponent, canActivate: [moduleGuard('AUDIT')] },
      { path: 'organizations', component: OrganizationsComponent, canActivate: [moduleGuard('ORGANIZATIONS')] },
      { path: 'cms', component: CmsComponent, canActivate: [moduleGuard('PLATFORM_CMS')] },
      { path: 'roles', component: RolesComponent, canActivate: [moduleGuard('ROLES')] },
      { path: 'settings', component: SettingsComponent, canActivate: [moduleGuard('SETTINGS')] },
    ],
  },
];
