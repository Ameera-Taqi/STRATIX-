import { Routes } from '@angular/router';
import { DashboardLayoutComponent } from './layout/dashboard-layout/dashboard-layout.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { ProjectsComponent } from './pages/projects/projects.component';
import { ProjectDetailComponent } from './pages/projects/project-detail.component';
import { TasksBoardComponent } from './pages/tasks/tasks-board.component';
import { TaskDetailComponent } from './pages/tasks/task-detail.component';
import { TeamComponent } from './pages/team/team.component';
import { EmployeeDetailComponent } from './pages/team/employee-detail.component';
import { PerformanceComponent } from './pages/performance/performance.component';
import { ReportsComponent } from './pages/reports/reports.component';
import { SettingsComponent } from './pages/settings/settings.component';
import { NotificationsComponent } from './pages/notifications/notifications.component';
import { RisksComponent } from './pages/risks/risks.component';
import { RiskDetailComponent } from './pages/risks/risk-detail.component';
import { TimelineComponent } from './pages/timeline/timeline.component';
import { AuditLogComponent } from './pages/audit-log/audit-log.component';
import { LoginComponent } from './pages/auth/login.component';
import { ForgotPasswordComponent } from './pages/auth/forgot-password.component';
import { ResetPasswordComponent } from './pages/auth/reset-password.component';
import { authGuard, guestGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'auth/login' },
  { path: 'auth/login', component: LoginComponent, canActivate: [guestGuard] },
  { path: 'auth/forgot-password', component: ForgotPasswordComponent, canActivate: [guestGuard] },
  { path: 'auth/reset-password', component: ResetPasswordComponent, canActivate: [guestGuard] },
  {
    path: '',
    component: DashboardLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: DashboardComponent },
      { path: 'projects', component: ProjectsComponent },
      { path: 'projects/:id', component: ProjectDetailComponent },
      { path: 'timeline', component: TimelineComponent },
      { path: 'tasks', component: TasksBoardComponent },
      { path: 'tasks/:id', component: TaskDetailComponent },
      { path: 'team', component: TeamComponent },
      { path: 'team/:id', component: EmployeeDetailComponent },
      { path: 'performance', component: PerformanceComponent },
      { path: 'reports', component: ReportsComponent },
      { path: 'risks', component: RisksComponent },
      { path: 'risks/:id', component: RiskDetailComponent },
      { path: 'notifications', component: NotificationsComponent },
      { path: 'audit', component: AuditLogComponent },
      { path: 'settings', component: SettingsComponent },
    ],
  },
];
