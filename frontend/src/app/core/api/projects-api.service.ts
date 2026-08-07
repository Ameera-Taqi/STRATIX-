import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateProjectRequest,
  ProjectRow,
  UpdateProjectRequest,
} from '../models/project.model';
import { CreateStageRequest, StageRow, UpdateStageRequest } from '../models/stage.model';
import { CreateTaskRequest, TaskCard, UpdateTaskRequest } from '../models/task.model';

@Injectable({ providedIn: 'root' })
export class ProjectsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  getProjects(): Observable<ProjectRow[]> {
    return this.http.get<ProjectRow[]>(`${this.base}/projects`);
  }

  getProject(id: number): Observable<ProjectRow> {
    return this.http.get<ProjectRow>(`${this.base}/projects/${id}`);
  }

  createProject(body: CreateProjectRequest): Observable<ProjectRow> {
    return this.http.post<ProjectRow>(`${this.base}/projects`, body);
  }

  updateProject(id: number, body: UpdateProjectRequest): Observable<ProjectRow> {
    return this.http.put<ProjectRow>(`${this.base}/projects/${id}`, body);
  }

  deleteProject(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/projects/${id}`);
  }

  getStages(projectId: number): Observable<StageRow[]> {
    return this.http.get<StageRow[]>(`${this.base}/projects/${projectId}/stages`);
  }

  createStage(projectId: number, body: CreateStageRequest): Observable<StageRow> {
    return this.http.post<StageRow>(`${this.base}/projects/${projectId}/stages`, body);
  }

  updateStage(id: number, body: UpdateStageRequest): Observable<StageRow> {
    return this.http.put<StageRow>(`${this.base}/stages/${id}`, body);
  }

  moveStage(id: number, direction: 'up' | 'down'): Observable<StageRow[]> {
    return this.http.patch<StageRow[]>(`${this.base}/stages/${id}/move`, { direction });
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

  createTask(body: CreateTaskRequest): Observable<TaskCard> {
    return this.http.post<TaskCard>(`${this.base}/tasks`, body);
  }

  updateTask(id: number, body: UpdateTaskRequest): Observable<TaskCard> {
    return this.http.put<TaskCard>(`${this.base}/tasks/${id}`, body);
  }

  deleteTask(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/tasks/${id}`);
  }
}
