import { HttpClient } from '@angular/common/http';

import { inject, Injectable } from '@angular/core';

import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';

import { LoginRequest, LoginResponse, RegisterOrganizationRequest, AuthUserProfile, ForgotPasswordRequest, ResetPasswordRequest, MessageResponse, UpdateMyProfileRequest, RefreshTokenRequest } from '../models/auth.model';

import {

  ProjectHealthAnalysisRequest,

  ProjectHealthAnalysisResponse,

} from '../models/ai-health-analysis.model';

import {

  CreateRiskRequest,
  ProjectRisk,

  RiskDashboardStats,

  RiskHeatMapData,
  UpdateRiskRequest,

} from '../models/risk.model';

import { CreateUserRequest, HealthResponse, UpdateUserRequest, User, UserDirectoryItem } from '../models/user.model';

import { CreateProjectRequest, ProjectRow, UpdateProjectRequest } from '../models/project.model';
import { CreateStageRequest, StageRow, UpdateStageRequest } from '../models/stage.model';
import { CreateTaskRequest, TaskCard, UpdateTaskRequest } from '../models/task.model';
import { AuthApiService } from '../api/auth-api.service';
import { UsersApiService } from '../api/users-api.service';
import { ProjectsApiService } from '../api/projects-api.service';

import { AuditLogApiRow, AuditLogPage } from '../models/audit.model';

import {
  CreateOrganizationRequest,
  OrganizationRow,
  PlanRow,
  UpdateOrganizationRequest,
} from '../models/organization.model';

import { mapAuditLogRow } from '../../shared/utils/audit-log.util';

import { map } from 'rxjs';

import {
  CreateNotificationRequest,
  NotificationApiResponse,
} from '../models/notification.model';

import {
  CreateProjectFileRequest,
  ProjectFileResponse,
} from '../models/project-file.model';

import { EmployeeKpiResponse } from '../models/employee-kpi.model';

import {
  CreateTaskCommentRequest,
  TaskCommentResponse,
} from '../models/task-comment.model';

import {
  CompanyAdminModulePermission,
  UpdateCompanyAdminPermissionsRequest,
} from '../models/cms.model';

import {
  CreateDepartmentRequest,
  DepartmentRow,
  UpdateDepartmentRequest,
} from '../models/department.model';

import {
  CreateOrganizationRoleRequest,
  OrganizationRoleRow,
  UpdateOrganizationRoleRequest,
} from '../models/organization-role.model';

import {
  OrganizationBranding,
  OrganizationLogoUploadResult,
} from '../models/branding.model';
import { CreateReportPayload, ReportResponse } from '../models/report.model';



@Injectable({ providedIn: 'root' })

export class ApiService {

  private readonly http = inject(HttpClient);
  private readonly authApi = inject(AuthApiService);
  private readonly usersApi = inject(UsersApiService);
  private readonly projectsApi = inject(ProjectsApiService);

  private readonly base = environment.apiUrl;



  health(): Observable<HealthResponse> {

    return this.http.get<HealthResponse>(`${this.base}/health`);

  }



  login(body: LoginRequest): Observable<LoginResponse> {
    return this.authApi.login(body);
  }

  registerOrganization(body: RegisterOrganizationRequest): Observable<LoginResponse> {
    return this.authApi.registerOrganization(body);
  }

  refresh(body: RefreshTokenRequest): Observable<LoginResponse> {
    return this.authApi.refresh(body);
  }

  logout(body: RefreshTokenRequest): Observable<void> {
    return this.authApi.logout(body);
  }

  forgotPassword(body: ForgotPasswordRequest): Observable<MessageResponse> {
    return this.authApi.forgotPassword(body);
  }

  resetPassword(body: ResetPasswordRequest): Observable<MessageResponse> {
    return this.authApi.resetPassword(body);
  }



  me(): Observable<AuthUserProfile> {
    return this.authApi.me();
  }

  updateMyProfile(body: UpdateMyProfileRequest): Observable<AuthUserProfile> {
    return this.authApi.updateMyProfile(body);
  }



  /** Full user administration list (OrgAdmins). Prefer getDirectoryUsers for pickers. */
  getUsers(): Observable<User[]> {
    return this.usersApi.getUsers();
  }

  /** Active tenant directory for assignee/manager pickers (all tenant roles). */
  getDirectoryUsers(): Observable<UserDirectoryItem[]> {
    return this.usersApi.getDirectoryUsers();
  }

  getDepartments(): Observable<DepartmentRow[]> {
    return this.http.get<DepartmentRow[]>(`${this.base}/departments`);
  }

  createDepartment(body: CreateDepartmentRequest): Observable<DepartmentRow> {
    return this.http.post<DepartmentRow>(`${this.base}/departments`, body);
  }

  updateDepartment(id: number, body: UpdateDepartmentRequest): Observable<DepartmentRow> {
    return this.http.put<DepartmentRow>(`${this.base}/departments/${id}`, body);
  }

  deleteDepartment(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/departments/${id}`);
  }

  getOrganizationRoles(): Observable<OrganizationRoleRow[]> {
    return this.http.get<OrganizationRoleRow[]>(`${this.base}/roles`);
  }

  createOrganizationRole(body: CreateOrganizationRoleRequest): Observable<OrganizationRoleRow> {
    return this.http.post<OrganizationRoleRow>(`${this.base}/roles`, body);
  }

  updateOrganizationRole(id: number, body: UpdateOrganizationRoleRequest): Observable<OrganizationRoleRow> {
    return this.http.put<OrganizationRoleRow>(`${this.base}/roles/${id}`, body);
  }

  deleteOrganizationRole(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/roles/${id}`);
  }

  getUser(id: number): Observable<User> {
    return this.usersApi.getUser(id);
  }

  createUser(body: CreateUserRequest): Observable<User> {
    return this.usersApi.createUser(body);
  }

  updateUser(id: number, body: UpdateUserRequest): Observable<User> {
    return this.usersApi.updateUser(id, body);
  }

  getAuditLogs(params: Record<string, string | number>): Observable<AuditLogPage> {

    return this.http

      .get<{
        content?: AuditLogApiRow[];
        items?: AuditLogApiRow[];
        totalElements?: number;
        total?: number;
        totalPages: number;
        page: number;
        size?: number;
        pageSize?: number;
      }>(`${this.base}/audit-logs`, { params })

      .pipe(

        map((response) => {
          const rows = response.content ?? response.items ?? [];
          return {
            content: rows.map((row) => mapAuditLogRow(row)),
            totalElements: response.totalElements ?? response.total ?? 0,
            totalPages: response.totalPages,
            page: response.page,
            size: response.size ?? response.pageSize ?? 25,
          };
        }),

      );

  }



  getProjects(): Observable<ProjectRow[]> {
    return this.projectsApi.getProjects();
  }

  getProject(id: number): Observable<ProjectRow> {
    return this.projectsApi.getProject(id);
  }

  createProject(body: CreateProjectRequest): Observable<ProjectRow> {
    return this.projectsApi.createProject(body);
  }

  updateProject(id: number, body: UpdateProjectRequest): Observable<ProjectRow> {
    return this.projectsApi.updateProject(id, body);
  }

  deleteProject(id: number): Observable<void> {
    return this.projectsApi.deleteProject(id);
  }

  getStages(projectId: number): Observable<StageRow[]> {
    return this.projectsApi.getStages(projectId);
  }

  createStage(projectId: number, body: CreateStageRequest): Observable<StageRow> {
    return this.projectsApi.createStage(projectId, body);
  }

  updateStage(id: number, body: UpdateStageRequest): Observable<StageRow> {
    return this.projectsApi.updateStage(id, body);
  }

  completeStage(id: number): Observable<StageRow> {
    return this.http.patch<StageRow>(`${this.base}/stages/${id}/complete`, {});
  }

  deleteStage(id: number): Observable<void> {
    return this.projectsApi.deleteStage(id);
  }

  getTasks(projectId?: number): Observable<TaskCard[]> {
    return this.projectsApi.getTasks(projectId);
  }

  getTask(id: number): Observable<TaskCard> {
    return this.projectsApi.getTask(id);
  }

  createTask(body: CreateTaskRequest): Observable<TaskCard> {
    return this.projectsApi.createTask(body);
  }

  updateTask(id: number, body: UpdateTaskRequest): Observable<TaskCard> {
    return this.projectsApi.updateTask(id, body);
  }

  updateTaskStatus(id: number, status: string): Observable<TaskCard> {
    return this.http.patch<TaskCard>(`${this.base}/tasks/${id}/status`, { status });
  }

  deleteTask(id: number): Observable<void> {
    return this.projectsApi.deleteTask(id);
  }



  getRisks(): Observable<ProjectRisk[]> {

    return this.http.get<ProjectRisk[]>(`${this.base}/risks`);

  }



  getRisk(id: number): Observable<ProjectRisk> {

    return this.http.get<ProjectRisk>(`${this.base}/risks/${id}`);

  }



  getRisksByProject(projectId: number): Observable<ProjectRisk[]> {

    return this.http.get<ProjectRisk[]>(`${this.base}/risks/project/${projectId}`);

  }



  getRisksByOwner(ownerId: number): Observable<ProjectRisk[]> {

    return this.http.get<ProjectRisk[]>(`${this.base}/risks/owner/${ownerId}`);

  }



  getRisksOpen(): Observable<ProjectRisk[]> {

    return this.http.get<ProjectRisk[]>(`${this.base}/risks/open`);

  }



  getRisksCritical(): Observable<ProjectRisk[]> {

    return this.http.get<ProjectRisk[]>(`${this.base}/risks/critical`);

  }



  getRiskDashboardStats(): Observable<RiskDashboardStats> {

    return this.http.get<RiskDashboardStats>(`${this.base}/risks/dashboard-stats`);

  }



  getRiskHeatMap(): Observable<RiskHeatMapData> {

    return this.http.get<RiskHeatMapData>(`${this.base}/risks/heat-map`);

  }



  createRisk(body: CreateRiskRequest): Observable<ProjectRisk> {

    return this.http.post<ProjectRisk>(`${this.base}/risks`, body);

  }



  updateRisk(id: number, body: UpdateRiskRequest): Observable<ProjectRisk> {

    return this.http.put<ProjectRisk>(`${this.base}/risks/${id}`, body);

  }



  deleteRisk(id: number): Observable<void> {

    return this.http.delete<void>(`${this.base}/risks/${id}`);

  }



  analyzeProjectHealth(body: ProjectHealthAnalysisRequest): Observable<ProjectHealthAnalysisResponse> {

    return this.http.post<ProjectHealthAnalysisResponse>(`${this.base}/ai/project-health-analysis`, body);

  }

  getOrganizations(): Observable<OrganizationRow[]> {
    return this.http.get<OrganizationRow[]>(`${this.base}/organizations`);
  }

  createOrganization(body: CreateOrganizationRequest): Observable<OrganizationRow> {
    return this.http.post<OrganizationRow>(`${this.base}/organizations`, body);
  }

  updateOrganization(id: number, body: UpdateOrganizationRequest): Observable<OrganizationRow> {
    return this.http.patch<OrganizationRow>(`${this.base}/organizations/${id}`, body);
  }

  getPlans(): Observable<PlanRow[]> {
    return this.http.get<PlanRow[]>(`${this.base}/plans`);
  }

  deleteOrganization(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/organizations/${id}`);
  }

  getNotifications(): Observable<NotificationApiResponse[]> {
    return this.http.get<NotificationApiResponse[]>(`${this.base}/notifications`);
  }

  createNotification(body: CreateNotificationRequest): Observable<NotificationApiResponse> {
    return this.http.post<NotificationApiResponse>(`${this.base}/notifications`, body);
  }

  markNotificationRead(id: number): Observable<void> {
    return this.http.patch<void>(`${this.base}/notifications/${id}/read`, {});
  }

  markAllNotificationsRead(): Observable<void> {
    return this.http.patch<void>(`${this.base}/notifications/read-all`, {});
  }

  deleteNotification(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/notifications/${id}`);
  }

  getProjectFiles(projectId: number): Observable<ProjectFileResponse[]> {
    return this.http.get<ProjectFileResponse[]>(`${this.base}/projects/${projectId}/files`);
  }

  createProjectFile(body: CreateProjectFileRequest): Observable<ProjectFileResponse> {
    return this.http.post<ProjectFileResponse>(`${this.base}/project-files`, body);
  }

  deleteProjectFile(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/project-files/${id}`);
  }

  getTaskComments(taskId: number): Observable<TaskCommentResponse[]> {
    return this.http.get<TaskCommentResponse[]>(`${this.base}/tasks/${taskId}/comments`);
  }

  createTaskComment(body: CreateTaskCommentRequest): Observable<TaskCommentResponse> {
    return this.http.post<TaskCommentResponse>(`${this.base}/task-comments`, body);
  }

  deleteTaskComment(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/task-comments/${id}`);
  }

  getEmployeeKpis(userId?: number): Observable<EmployeeKpiResponse[]> {
    const url =
      userId != null
        ? `${this.base}/employee-kpis?userId=${userId}`
        : `${this.base}/employee-kpis`;
    return this.http.get<EmployeeKpiResponse[]>(url);
  }

  getCompanyAdminPermissions(): Observable<CompanyAdminModulePermission[]> {
    return this.http.get<CompanyAdminModulePermission[]>(`${this.base}/cms/company-admin-permissions`);
  }

  updateCompanyAdminPermissions(
    body: UpdateCompanyAdminPermissionsRequest,
  ): Observable<CompanyAdminModulePermission[]> {
    return this.http.put<CompanyAdminModulePermission[]>(`${this.base}/cms/company-admin-permissions`, body);
  }

  getOrganizationBranding(): Observable<OrganizationBranding> {
    return this.http.get<OrganizationBranding>(`${this.base}/organization/branding`);
  }

  uploadOrganizationLogo(file: File): Observable<OrganizationLogoUploadResult> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<OrganizationLogoUploadResult>(`${this.base}/organization/logo`, form);
  }

  clearOrganizationLogo(): Observable<void> {
    return this.http.delete<void>(`${this.base}/organization/logo`);
  }

  getReports(): Observable<ReportResponse[]> {
    return this.http.get<ReportResponse[]>(`${this.base}/reports`);
  }

  createReport(meta: CreateReportPayload, file: File): Observable<ReportResponse> {
    const form = new FormData();
    form.append('Title', meta.title);
    form.append('ReportType', meta.reportType);
    form.append('Format', meta.format);
    if (meta.projectId != null) form.append('ProjectId', String(meta.projectId));
    if (meta.departmentId != null) form.append('DepartmentId', String(meta.departmentId));
    if (meta.employeeId != null) form.append('EmployeeId', String(meta.employeeId));
    if (meta.dateFrom) form.append('DateFrom', meta.dateFrom);
    if (meta.dateTo) form.append('DateTo', meta.dateTo);
    form.append('file', file, file.name);
    return this.http.post<ReportResponse>(`${this.base}/reports`, form);
  }

  deleteReport(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/reports/${id}`);
  }

  downloadReport(id: number): Observable<Blob> {
    return this.http.get(`${this.base}/reports/${id}/download`, { responseType: 'blob' });
  }

}


