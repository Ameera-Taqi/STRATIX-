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

import { mapAuditLogRow } from '../../shared/utils/audit-log.util';

import { map } from 'rxjs';



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

  getDepartments(): Observable<{ id: number; name: string }[]> {

    return this.http.get<{ id: number; name: string }[]>(`${this.base}/departments`);

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

}


