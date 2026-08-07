import { Injectable, inject, signal } from '@angular/core';
import { Observable, map, tap, catchError, throwError } from 'rxjs';

import { ProjectRow, StageRow } from '../data/mock-data';

import { User } from '../models/user.model';

import { ApiService } from './api.service';
import { EmployeesStore } from './employees.store';
import { NotificationsStore } from './notifications.store';
import { asDateString, normalizeProjectRow, normalizeStageRow } from './project-normalize';

import {

  progressFromStages,

  progressFromTasks,

  stageProgressFromTasks,

  TaskProgressInput,

} from '../../shared/utils/project-progress.util';



export interface NewProjectForm {
  name: string;
  description?: string | null;
  department: string;
  manager: string;
  managerId?: number | null;
  startDate: string;
  endDate: string;
  status: string;
  priority: string;
}



export interface NewStageForm {
  name: string;
  description?: string | null;
  startDate: string;
  endDate: string;
  status?: string;
}



@Injectable({ providedIn: 'root' })

export class ProjectsStore {

  private readonly api = inject(ApiService);

  private readonly employeesStore = inject(EmployeesStore);

  private readonly notifications = inject(NotificationsStore);



  private readonly _projects = signal<ProjectRow[]>([]);

  private readonly _stagesByProject = signal<Record<number, StageRow[]>>({});

  private readonly _loaded = signal(false);

  private readonly _loading = signal(false);

  private readonly _loadError = signal<string | null>(null);

  private readonly _users = signal<User[]>([]);

  private loadGeneration = 0;



  readonly projects = this._projects.asReadonly();
  readonly stagesByProject = this._stagesByProject.asReadonly();
  readonly loaded = this._loaded.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly loadError = this._loadError.asReadonly();
  readonly users = this._users.asReadonly();



  loadFromApi(onReady?: () => void): void {
    const generation = ++this.loadGeneration;
    this._loading.set(true);
    this._loadError.set(null);

    this.api.getDirectoryUsers().subscribe({

      next: (users) => {
        if (generation !== this.loadGeneration) return;
        this._users.set(users as User[]);
        this.loadProjects(generation, onReady);
      },

      error: () => {
        if (generation !== this.loadGeneration) return;
        this.loadProjects(generation, onReady);
      },

    });

  }



  private loadProjects(generation: number, onReady?: () => void): void {

    this.api.getProjects().subscribe({

      next: (projects) => {
        if (generation !== this.loadGeneration) return;

        const list = Array.isArray(projects) ? projects : [];
        this._projects.set(list.map((p) => this.normalizeProject(p)));
        this._loaded.set(true);
        this._loading.set(false);
        this._loadError.set(null);
        this.syncEmployeeProjectStats();
        this.loadAllStages(list.map((p) => p.id), generation, onReady);
      },

      error: () => {
        if (generation !== this.loadGeneration) return;

        // Keep any previously loaded projects so navigation to detail does not go blank.
        this._loaded.set(true);
        this._loading.set(false);
        this._loadError.set('projects.loadError');
        onReady?.();
      },

    });

  }



  private loadAllStages(projectIds: number[], generation: number, onReady?: () => void): void {
    if (projectIds.length === 0) {
      if (generation !== this.loadGeneration) return;
      this._stagesByProject.set({});
      onReady?.();
      return;
    }

    let pending = projectIds.length;

    for (const projectId of projectIds) {
      this.api.getStages(projectId).subscribe({
        next: (stages) => {
          if (generation !== this.loadGeneration) return;
          this._stagesByProject.update((map) => ({
            ...map,
            [projectId]: stages.map((s) => this.normalizeStage(s)),
          }));
          pending -= 1;
          if (pending === 0) {
            onReady?.();
          }
        },
        error: () => {
          if (generation !== this.loadGeneration) return;
          pending -= 1;
          if (pending === 0) {
            onReady?.();
          }
        },
      });
    }
  }



  getStages(projectId: number): StageRow[] {
    const key = Number(projectId);
    const list = this._stagesByProject()[key] ?? [];
    return [...list].sort((a, b) => a.orderNumber - b.orderNumber);
  }



  addStage(projectId: number, form: NewStageForm): Observable<StageRow> {
    const project = this.getById(projectId);
    const tempId = -Date.now();
    const body = {
      name: form.name.trim(),
      description: form.description?.trim() || null,
      startDate: form.startDate || null,
      endDate: form.endDate || null,
      status: this.toStageStatusEnum(form.status || 'PLANNED'),
    };

    const optimistic: StageRow = {
      id: tempId,
      name: form.name.trim(),
      description: form.description?.trim() || null,
      startDate: form.startDate,
      endDate: form.endDate,
      progress: 0,
      status: form.status || 'Planned',
      orderNumber: this.getStages(projectId).length + 1,
    };

    this._stagesByProject.update((map) => ({
      ...map,
      [projectId]: [...(map[projectId] ?? []), optimistic],
    }));

    return this.api.createStage(projectId, body).pipe(
      map((stage) => {
        const row = this.normalizeStage(stage);
        this._stagesByProject.update((m) => ({
          ...m,
          [projectId]: (m[projectId] ?? []).map((s) => (s.id === tempId ? row : s)),
        }));
        this.syncProjectProgress(projectId);
        this.notifications.push({
          titleKey: 'notifications.featureAddedTitle',
          bodyKey: 'notifications.featureAddedBody',
          params: { feature: row.name, project: project?.name ?? '' },
        });
        return row;
      }),
      catchError((err) => {
        this._stagesByProject.update((m) => ({
          ...m,
          [projectId]: (m[projectId] ?? []).filter((s) => s.id !== tempId),
        }));
        return throwError(() => err);
      }),
    );
  }

  reloadStages(projectId: number): void {
    this.api.getStages(projectId).subscribe({
      next: (stages) => {
        this._stagesByProject.update((map) => ({
          ...map,
          [projectId]: stages.map((s) => this.normalizeStage(s)),
        }));
        // Do not average stage % into project — project progress is task-effort derived.
      },
    });
  }



  completeStage(projectId: number, stageId: number): void {

    const stage = this.getStages(projectId).find((s) => s.id === stageId);

    if (!stage || stage.status === 'Done') return;



    const project = this.getById(projectId);

    this._stagesByProject.update((map) => ({

      ...map,

      [projectId]: (map[projectId] ?? []).map((s) =>

        s.id === stageId ? { ...s, status: 'Done', progress: 100 } : s,

      ),

    }));

    this.syncProjectProgress(projectId);



    this.api.completeStage(stageId).subscribe({

      next: (updated) => {

        this._stagesByProject.update((map) => ({

          ...map,

          [projectId]: (map[projectId] ?? []).map((s) =>

            s.id === stageId ? this.normalizeStage(updated) : s,

          ),

        }));

        this.syncProjectProgress(projectId);

      },

    });



    this.notifications.push({

      titleKey: 'notifications.featureCompletedTitle',

      bodyKey: 'notifications.featureCompletedBody',

      params: { feature: stage.name, project: project?.name ?? '' },

    });

  }



  removeStage(projectId: number, stageId: number): void {
    const updated = this.getStages(projectId)
      .filter((s) => s.id !== stageId)
      .map((s, i) => ({ ...s, orderNumber: i + 1 }));
    this._stagesByProject.update((map) => ({ ...map, [projectId]: updated }));
    this.syncProjectProgress(projectId);
    this.api.deleteStage(stageId).subscribe({ error: () => {} });
  }

  /** Display-list reorder only (order_number). Not a schedule dependency — features may run in parallel. */
  moveStage(projectId: number, stageId: number, direction: 'up' | 'down'): void {
    const stages = this.getStages(projectId);
    const idx = stages.findIndex((s) => s.id === stageId);
    const swapIdx = direction === 'up' ? idx - 1 : idx + 1;
    if (idx < 0 || swapIdx < 0 || swapIdx >= stages.length) return;

    // Optimistic local swap
    const next = [...stages];
    const a = next[idx];
    const b = next[swapIdx];
    next[idx] = { ...b, orderNumber: a.orderNumber };
    next[swapIdx] = { ...a, orderNumber: b.orderNumber };
    this._stagesByProject.update((map) => ({
      ...map,
      [projectId]: next.sort((x, y) => x.orderNumber - y.orderNumber),
    }));

    this.api.moveStage(stageId, direction).subscribe({
      next: (rows) => {
        this._stagesByProject.update((map) => ({
          ...map,
          [projectId]: rows.map((s) => this.normalizeStage(s)),
        }));
      },
      error: () => this.reloadStages(projectId),
    });
  }

  private syncProjectProgress(projectId: number): void {
    const stages = this.getStages(projectId);
    // No stages → 0 (defined). Prefer task-driven sync when tasks are available.
    this.setProjectProgress(projectId, progressFromStages(stages));
  }



  syncProgressFromTasks(projectId: number, tasks: TaskProgressInput[]): void {

    const stages = this.getStages(projectId);

    const tasksByStage = new Map<number, TaskProgressInput[]>();



    for (const task of tasks) {

      if (task.stageId == null) continue;

      const list = tasksByStage.get(task.stageId) ?? [];

      list.push(task);

      tasksByStage.set(task.stageId, list);

    }



    // Always derive every stage from its tasks (empty stage → 0; never keep stale %).
    const updatedStages = stages.map((stage) => {
      const stageTasks = tasksByStage.get(stage.id) ?? [];
      const progress = stageProgressFromTasks(stageTasks);
      // Empty stage stays at its status (0% is normal). Only reopen Done when tasks exist and are incomplete.
      let status = stage.status;
      if (progress >= 100) status = 'Done';
      else if (stage.status === 'Done' && stageTasks.length > 0) status = 'Active';
      return { ...stage, progress, status };
    });

    this._stagesByProject.update((map) => ({ ...map, [projectId]: updatedStages }));

    // Effort-weighted tasks are the source of truth (matches backend).
    // No tasks → 0 (normal for PLANNED; never null / divide-by-zero).
    this.setProjectProgress(projectId, progressFromTasks(tasks));
  }



  private setProjectProgress(projectId: number, progress: number): void {
    const target = Number(projectId);
    this._projects.update((list) =>
      list.map((p) => (Number(p.id) === target ? { ...p, progress } : p)),
    );
  }



  addProject(form: NewProjectForm): Observable<ProjectRow> {
    const managerId = form.managerId ?? this.resolveManagerId(form.manager);
    const managerName = this.resolveManagerName(managerId, form.manager);
    const body = {
      name: form.name.trim(),
      description: form.description?.trim() || null,
      departmentId: this.resolveDepartmentId(form.department),
      projectManagerId: managerId,
      startDate: form.startDate || null,
      endDate: form.endDate || null,
      status: this.toProjectStatusEnum(form.status || 'ACTIVE'),
      priority: this.toProjectPriorityEnum(form.priority),
      progress: 0,
    };

    const tempId = -Date.now();
    const optimistic: ProjectRow = {
      id: tempId,
      name: form.name.trim(),
      department: form.department,
      manager: managerName,
      managerId,
      startDate: form.startDate,
      endDate: form.endDate,
      progress: 0,
      status: form.status,
      priority: this.toProjectPriorityEnum(form.priority) as ProjectRow['priority'],
      owner: managerName,
      deadline: form.endDate,
    };

    this._projects.update((list) => [...list, optimistic]);
    this._stagesByProject.update((map) => ({ ...map, [tempId]: [] }));
    this.syncEmployeeProjectStats();

    return this.api.createProject(body).pipe(
      map((created) => this.normalizeProject({
        ...created,
        manager: created.manager?.trim() || managerName,
        owner: created.owner?.trim() || created.manager?.trim() || managerName,
        managerId: created.managerId ?? managerId,
      })),
      tap((row) => {
        this._projects.update((list) =>
          list.map((p) => (p.id === tempId ? row : p)),
        );
        this._stagesByProject.update((map) => {
          const stages = map[tempId] ?? [];
          const { [tempId]: _, ...rest } = map;
          return { ...rest, [row.id]: stages };
        });
        this.notifications.push({
          titleKey: 'notifications.projectCreatedTitle',
          bodyKey: 'notifications.projectCreatedBody',
          params: { project: row.name },
        });
        this.syncEmployeeProjectStats();
      }),
      catchError((err) => {
        this._projects.update((list) => list.filter((p) => p.id !== tempId));
        this._stagesByProject.update((map) => {
          const { [tempId]: _, ...rest } = map;
          return rest;
        });
        this.syncEmployeeProjectStats();
        return throwError(() => err);
      }),
    );
  }



  getById(id: number): ProjectRow | undefined {
    const target = Number(id);
    return this._projects().find((p) => Number(p.id) === target);
  }

  /** Load/refresh a single project (and its features) for the detail page. */
  ensureProject(id: number, onReady?: () => void): void {
    const projectId = Number(id);
    if (!Number.isFinite(projectId) || projectId <= 0) {
      onReady?.();
      return;
    }

    // Always refresh from API so View opens the exact project that was clicked.
    this._loading.set(true);
    this.api.getProject(projectId).subscribe({
      next: (project) => {
        const normalized = this.normalizeProject(project);
        this._projects.update((list) => {
          const without = list.filter((p) => Number(p.id) !== Number(normalized.id));
          return [...without, normalized];
        });
        this._loaded.set(true);
        this._loading.set(false);
        this._loadError.set(null);
        this.syncEmployeeProjectStats();
        this.api.getStages(projectId).subscribe({
          next: (stages) => {
            this._stagesByProject.update((map) => ({
              ...map,
              [projectId]: stages.map((s) => this.normalizeStage(s)),
            }));
            onReady?.();
          },
          error: () => onReady?.(),
        });
      },
      error: () => {
        this._loading.set(false);
        if (this.getById(projectId)) {
          onReady?.();
          return;
        }
        this._loadError.set('projects.loadError');
        onReady?.();
      },
    });
  }

  reloadUsers(): void {
    this.api.getDirectoryUsers().subscribe({
      next: (users) => {
        this._users.set(users as User[]);
        const namesById = new Map(users.map((u) => [u.id, u.name]));
        this._projects.update((list) =>
          list.map((p) => {
            if (p.managerId == null) return p;
            const name = namesById.get(p.managerId);
            return name ? { ...p, manager: name, owner: name } : p;
          }),
        );
        this.syncEmployeeProjectStats();
      },
    });
  }

  deleteProject(id: number): Observable<void> {
    const project = this.getById(id);
    this._projects.update((list) => list.filter((p) => p.id !== id));
    this._stagesByProject.update((map) => {
      const { [id]: _, ...rest } = map;
      return rest;
    });
    this.syncEmployeeProjectStats();

    return this.api.deleteProject(id).pipe(
      tap(() => {
        if (project) {
          this.notifications.push({
            titleKey: 'notifications.projectDeletedTitle',
            bodyKey: 'notifications.projectDeletedBody',
            params: { project: project.name },
          });
        }
        this.syncEmployeeProjectStats();
      }),
      catchError((err) => {
        this.loadFromApi();
        return throwError(() => err);
      }),
    );
  }



  private normalizeProject(project: ProjectRow): ProjectRow {
    return normalizeProjectRow(project);
  }

  private syncEmployeeProjectStats(): void {
    this.employeesStore.syncFromProjects(this._projects());
  }

  private normalizeStage(stage: StageRow): StageRow {
    return normalizeStageRow(stage);
  }

  private asDateString(value: string | null | undefined): string {
    return asDateString(value);
  }

  private resolveDepartmentId(name: string): number {

    const match = this._users().find((u) => u.departmentName === name);

    return match?.departmentId ?? this._users()[0]?.departmentId ?? 1;

  }



  private resolveManagerId(name: string): number {
    const trimmed = name.trim().toLowerCase();
    const match = this._users().find((u) => u.name.trim().toLowerCase() === trimmed);
    return match?.id ?? this._users()[0]?.id ?? 1;
  }

  private resolveManagerName(managerId: number, fallback: string): string {
    const user = this._users().find((u) => u.id === managerId);
    return user?.name ?? fallback.trim();
  }



  private toProjectStatusEnum(status: string): string {

    const map: Record<string, string> = {

      Planned: 'PLANNED',

      Active: 'ACTIVE',

      'On Hold': 'ON_HOLD',

      Review: 'ON_HOLD',

      Delayed: 'ON_HOLD',

      Done: 'COMPLETED',

      Cancelled: 'CANCELLED',

    };

    return map[status] ?? 'PLANNED';

  }



  private toProjectPriorityEnum(priority: string): string {

    if (priority === 'URGENT') return 'CRITICAL';

    return priority;

  }



  private toStageStatusEnum(status: string): string {

    const map: Record<string, string> = {

      Planned: 'PLANNED',

      Active: 'ACTIVE',

      Done: 'DONE',

      'On Hold': 'ON_HOLD',

    };

    return map[status] ?? 'PLANNED';

  }

}


