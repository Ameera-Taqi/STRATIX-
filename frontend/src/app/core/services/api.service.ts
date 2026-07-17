import { HttpClient } from '@angular/common/http';

import { inject, Injectable } from '@angular/core';

import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';

import { LoginRequest, LoginResponse, RegisterOrganizationRequest, AuthUserProfile, ForgotPasswordRequest, ResetPasswordRequest, MessageResponse, UpdateMyProfileRequest } from '../models/auth.model';

import {

  ProjectHealthAnalysisRequest,

  ProjectHealthAnalysisResponse,

} from '../models/ai-health-analysis.model';

import {

  ProjectRisk,

  RiskDashboardStats,

  RiskHeatMapData,

} from '../models/risk.model';

import { HealthResponse, User } from '../models/user.model';

import { ProjectRow, StageRow, TaskCard } from '../data/mock-data';

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

import {
  CreateMilestoneRequest,
  MilestoneResponse,
  UpdateMilestoneRequest,
} from '../models/milestone.model';

import {
  ChangeRequestResponse,
  CreateChangeRequestRequest,
  UpdateChangeRequestRequest,
} from '../models/change-request.model';

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



@Injectable({ providedIn: 'root' })

export class ApiService {

  private readonly http = inject(HttpClient);

  private readonly base = environment.apiUrl;



  health(): Observable<HealthResponse> {

    return this.http.get<HealthResponse>(`${this.base}/health`);

  }



  login(body: LoginRequest): Observable<LoginResponse> {

    return this.http.post<LoginResponse>(`${this.base}/auth/login`, body);

  }

  registerOrganization(body: RegisterOrganizationRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.base}/auth/register`, body);
  }

  forgotPassword(body: ForgotPasswordRequest): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`${this.base}/auth/forgot-password`, body);
  }

  resetPassword(body: ResetPasswordRequest): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`${this.base}/auth/reset-password`, body);
  }



  me(): Observable<AuthUserProfile> {

    return this.http.get<AuthUserProfile>(`${this.base}/auth/me`);

  }

  updateMyProfile(body: UpdateMyProfileRequest): Observable<AuthUserProfile> {
    return this.http.patch<AuthUserProfile>(`${this.base}/auth/me`, body);
  }



  getUsers(): Observable<User[]> {

    return this.http.get<User[]>(`${this.base}/users`);

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

    return this.http.get<User>(`${this.base}/users/${id}`);

  }

  createUser(body: unknown): Observable<User> {

    return this.http.post<User>(`${this.base}/users`, body);

  }

  updateUser(id: number, body: unknown): Observable<User> {

    return this.http.put<User>(`${this.base}/users/${id}`, body);

  }

  getAuditLogs(params: Record<string, string | number>): Observable<AuditLogPage> {

    return this.http

      .get<{

        content: AuditLogApiRow[];

        totalElements: number;

        totalPages: number;

        page: number;

        size: number;

      }>(`${this.base}/audit-logs`, { params })

      .pipe(

        map((response) => ({

          content: response.content.map((row) => mapAuditLogRow(row)),

          totalElements: response.totalElements,

          totalPages: response.totalPages,

          page: response.page,

          size: response.size,

        })),

      );

  }



  getProjects(): Observable<ProjectRow[]> {

    return this.http.get<ProjectRow[]>(`${this.base}/projects`);

  }



  getProject(id: number): Observable<ProjectRow> {

    return this.http.get<ProjectRow>(`${this.base}/projects/${id}`);

  }



  createProject(body: unknown): Observable<ProjectRow> {

    return this.http.post<ProjectRow>(`${this.base}/projects`, body);

  }



  updateProject(id: number, body: unknown): Observable<ProjectRow> {

    return this.http.put<ProjectRow>(`${this.base}/projects/${id}`, body);

  }



  deleteProject(id: number): Observable<void> {

    return this.http.delete<void>(`${this.base}/projects/${id}`);

  }



  getStages(projectId: number): Observable<StageRow[]> {

    return this.http.get<StageRow[]>(`${this.base}/projects/${projectId}/stages`);

  }



  createStage(projectId: number, body: unknown): Observable<StageRow> {

    return this.http.post<StageRow>(`${this.base}/projects/${projectId}/stages`, body);

  }



  updateStage(id: number, body: unknown): Observable<StageRow> {

    return this.http.put<StageRow>(`${this.base}/stages/${id}`, body);

  }



  completeStage(id: number): Observable<StageRow> {

    return this.http.patch<StageRow>(`${this.base}/stages/${id}/complete`, {});

  }



  deleteStage(id: number): Observable<void> {

    return this.http.delete<void>(`${this.base}/stages/${id}`);

  }



  getTasks(projectId?: number): Observable<TaskCard[]> {

    const url =

      projectId != null

        ? `${this.base}/tasks?projectId=${projectId}`

        : `${this.base}/tasks`;

    return this.http.get<TaskCard[]>(url);

  }



  getTask(id: number): Observable<TaskCard> {

    return this.http.get<TaskCard>(`${this.base}/tasks/${id}`);

  }



  createTask(body: unknown): Observable<TaskCard> {

    return this.http.post<TaskCard>(`${this.base}/tasks`, body);

  }



  updateTask(id: number, body: unknown): Observable<TaskCard> {

    return this.http.put<TaskCard>(`${this.base}/tasks/${id}`, body);

  }



  updateTaskStatus(id: number, status: string): Observable<TaskCard> {

    return this.http.patch<TaskCard>(`${this.base}/tasks/${id}/status`, { status });

  }



  deleteTask(id: number): Observable<void> {

    return this.http.delete<void>(`${this.base}/tasks/${id}`);

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



  createRisk(body: unknown): Observable<ProjectRisk> {

    return this.http.post<ProjectRisk>(`${this.base}/risks`, body);

  }



  updateRisk(id: number, body: unknown): Observable<ProjectRisk> {

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

  getMilestones(projectId?: number): Observable<MilestoneResponse[]> {
    const url =
      projectId != null
        ? `${this.base}/projects/${projectId}/milestones`
        : `${this.base}/milestones`;
    return this.http.get<MilestoneResponse[]>(url);
  }

  createMilestone(body: CreateMilestoneRequest): Observable<MilestoneResponse> {
    return this.http.post<MilestoneResponse>(`${this.base}/milestones`, body);
  }

  updateMilestone(id: number, body: UpdateMilestoneRequest): Observable<MilestoneResponse> {
    return this.http.put<MilestoneResponse>(`${this.base}/milestones/${id}`, body);
  }

  deleteMilestone(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/milestones/${id}`);
  }

  getChangeRequests(projectId?: number): Observable<ChangeRequestResponse[]> {
    const url =
      projectId != null
        ? `${this.base}/change-requests?projectId=${projectId}`
        : `${this.base}/change-requests`;
    return this.http.get<ChangeRequestResponse[]>(url);
  }

  createChangeRequest(body: CreateChangeRequestRequest): Observable<ChangeRequestResponse> {
    return this.http.post<ChangeRequestResponse>(`${this.base}/change-requests`, body);
  }

  updateChangeRequest(id: number, body: UpdateChangeRequestRequest): Observable<ChangeRequestResponse> {
    return this.http.put<ChangeRequestResponse>(`${this.base}/change-requests/${id}`, body);
  }

  deleteChangeRequest(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/change-requests/${id}`);
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

}


