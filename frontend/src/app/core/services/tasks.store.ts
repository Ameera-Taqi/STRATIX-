import { Injectable, inject, signal } from '@angular/core';

import { TaskCard } from '../data/mock-data';

import { ApiService } from './api.service';

import { EmployeesStore } from './employees.store';

import { NotificationsStore } from './notifications.store';

import { ProjectsStore } from './projects.store';

import { tasksForEmployee } from '../../shared/utils/employee-stats.util';



export interface NewTaskForm {
  projectId: number;
  title: string;
  assignee: string;
  assigneeId?: number | null;
  priority: TaskCard['priority'];
  startDate?: string;
  dueDate: string;
  status: TaskCard['status'];
  stageId?: number | null;
  description?: string;
  estimatedHours: number;
}



@Injectable({ providedIn: 'root' })

export class TasksStore {

  private readonly api = inject(ApiService);

  private readonly notifications = inject(NotificationsStore);

  private readonly employeesStore = inject(EmployeesStore);

  private readonly projectsStore = inject(ProjectsStore);



  private readonly _tasks = signal<TaskCard[]>([]);

  private readonly _loaded = signal(false);



  readonly tasks = this._tasks.asReadonly();

  readonly loaded = this._loaded.asReadonly();



  loadFromApi(): void {

    this.api.getTasks().subscribe({

      next: (tasks) => {

        this._tasks.set(tasks.map((t) => this.normalizeTask(t)));

        this._loaded.set(true);

        this.syncDerivedState();

      },

      error: () => this._loaded.set(true),

    });

  }



  private syncDerivedState(): void {
    // Do not recompute project.progress on load — that overwrites the API value
    // (e.g. 30%) with task-completion % (0% when no tasks are DONE yet).
    this.employeesStore.syncFromTasks(this._tasks());
    this.employeesStore.syncFromProjects(this.projectsStore.projects());
  }



  private syncAllProjectProgress(): void {

    const projectIds = [...new Set(this._tasks().map((t) => t.projectId))];

    for (const projectId of projectIds) {

      this.refreshProjectProgress(projectId);

    }

  }



  private refreshProjectProgress(projectId: number): void {

    this.projectsStore.syncProgressFromTasks(projectId, this.getByProject(projectId));

  }



  getAll(): TaskCard[] {

    return this._tasks();

  }



  getByProject(projectId: number): TaskCard[] {
    const target = Number(projectId);
    return this._tasks().filter((t) => Number(t.projectId) === target);
  }

  getById(id: number): TaskCard | undefined {
    const target = Number(id);
    return this._tasks().find((t) => Number(t.id) === target);
  }



  getByAssignee(assigneeName: string): TaskCard[] {

    const employee = this.employeesStore

      .getAll()

      .find((e) => e.name.trim().toLowerCase() === assigneeName.trim().toLowerCase());

    if (employee) return this.getByEmployee(employee.id, employee.name);

    const name = assigneeName.trim().toLowerCase();

    return this._tasks().filter((t) => t.assignee.trim().toLowerCase() === name);

  }



  getByEmployee(employeeId: number, employeeName: string): TaskCard[] {

    return tasksForEmployee(employeeId, employeeName, this._tasks());

  }



  private resolveAssignee(form: Pick<NewTaskForm, 'assignee' | 'assigneeId'>): {

    assignee: string;

    assigneeId: number | null;

  } {

    if (form.assigneeId != null) {

      const employee = this.employeesStore.getById(form.assigneeId);

      if (employee) {

        return { assignee: employee.name, assigneeId: employee.id };

      }

    }

    const name = form.assignee.trim();

    const employee = this.employeesStore

      .getAll()

      .find((e) => e.name.trim().toLowerCase() === name.toLowerCase());

    return { assignee: name, assigneeId: employee?.id ?? null };

  }



  addTask(form: NewTaskForm): TaskCard {

    const project = this.projectsStore.getById(form.projectId);

    const stages = this.projectsStore.getStages(form.projectId);

    const stage = form.stageId ? stages.find((s) => s.id === form.stageId) : undefined;

    const { assignee, assigneeId } = this.resolveAssignee(form);



    const estimatedHours = Number(form.estimatedHours) > 0 ? Number(form.estimatedHours) : 1;

    const body = {
      projectId: form.projectId,
      stageId: form.stageId ?? null,
      title: form.title.trim(),
      description: form.description?.trim() || null,
      status: form.status,
      priority: form.priority,
      assigneeId,
      startDate: form.startDate || null,
      dueDate: form.dueDate || null,
      estimatedHours,
    };

    const task: TaskCard = {
      id: -Date.now(),
      projectId: form.projectId,
      projectName: project?.name ?? 'Project',
      stageId: form.stageId ?? null,
      stageName: stage?.name ?? null,
      title: form.title.trim(),
      assignee,
      assigneeId,
      priority: form.priority,
      startDate: form.startDate || '',
      dueDate: form.dueDate,
      status: form.status,
      description: form.description,
      estimatedHours,
    };



    this._tasks.update((list) => [...list, task]);

    this.notifications.push({
      titleKey: 'notifications.taskCreatedTitle',
      bodyKey: 'notifications.taskCreatedBody',
      params: { task: task.title, project: task.projectName },
      link: `/tasks/${task.id}`,
      entityType: 'TASK',
      entityId: task.id,
      entityLabel: task.title,
      projectName: task.projectName,
    });

    this.refreshProjectProgress(task.projectId);

    this.employeesStore.syncFromTasks(this._tasks());

    this.employeesStore.syncFromProjects(this.projectsStore.projects());



    this.api.createTask(body).subscribe({

      next: (created) => {

        const row = this.normalizeTask(created);

        this._tasks.update((list) => list.map((t) => (t.id === task.id ? row : t)));

        this.refreshProjectProgress(row.projectId);

        this.employeesStore.syncFromTasks(this._tasks());

        this.employeesStore.syncFromProjects(this.projectsStore.projects());

      },

    });



    return task;

  }



  moveTask(
    taskId: number,
    newStatus: TaskCard['status'],
    reasons?: { blockedReason?: string; reopenReason?: string; reviewReason?: string },
  ): void {
    const task = this.getById(taskId);
    if (!task || task.status === newStatus) return;

    const blockedReason = reasons?.blockedReason?.trim();
    const reopenReason = reasons?.reopenReason?.trim();
    const reviewReason = reasons?.reviewReason?.trim();

    if (newStatus === 'BLOCKED' && !blockedReason) return;
    if (task.status === 'DONE' && !reopenReason) return;
    if (task.status === 'REVIEW' && newStatus === 'IN_PROGRESS' && !reviewReason) return;

    const payload = {
      blockedReason: blockedReason || undefined,
      reopenReason: reopenReason || undefined,
      reviewReason: reviewReason || undefined,
    };

    const previousStatus = task.status;
    const previousMeta = {
      reviewReason: task.reviewReason,
      submittedForReviewAt: task.submittedForReviewAt,
      submittedForReviewById: task.submittedForReviewById,
      submittedForReviewBy: task.submittedForReviewBy,
    };

    this._tasks.update((list) =>
      list.map((t) =>
        t.id === taskId
          ? {
              ...t,
              status: newStatus,
              blockedReason: newStatus === 'BLOCKED' ? blockedReason ?? null : t.blockedReason,
              reopenReason: previousStatus === 'DONE' ? reopenReason ?? null : t.reopenReason,
              reviewReason:
                newStatus === 'REVIEW' || (previousStatus === 'REVIEW' && newStatus === 'IN_PROGRESS')
                  ? reviewReason ?? t.reviewReason
                  : t.reviewReason,
              submittedForReviewAt: newStatus === 'REVIEW' ? new Date().toISOString() : null,
              submittedForReviewBy:
                newStatus === 'REVIEW' ? t.submittedForReviewBy ?? t.assignee : null,
              submittedForReviewById:
                newStatus === 'REVIEW' ? t.submittedForReviewById ?? t.assigneeId : null,
            }
          : t,
      ),
    );

    this.refreshProjectProgress(task.projectId);
    this.employeesStore.syncFromTasks(this._tasks());
    this.employeesStore.syncFromProjects(this.projectsStore.projects());

    this.api.updateTaskStatus(taskId, newStatus, payload).subscribe({
      next: (updated) => {
        this._tasks.update((list) =>
          list.map((t) => (t.id === taskId ? this.normalizeTask(updated) : t)),
        );
        this.refreshProjectProgress(updated.projectId);
        this.projectsStore.reloadStages(updated.projectId);
      },
      error: () => {
        this._tasks.update((list) =>
          list.map((t) =>
            t.id === taskId ? { ...t, status: previousStatus, ...previousMeta } : t,
          ),
        );
        this.refreshProjectProgress(task.projectId);
        this.employeesStore.syncFromTasks(this._tasks());
        this.employeesStore.syncFromProjects(this.projectsStore.projects());
      },
    });
  }



  updateTask(
    taskId: number,
    patch: Partial<Pick<TaskCard, 'title' | 'assignee' | 'assigneeId' | 'priority' | 'dueDate' | 'description' | 'stageId'>>,
    onDone?: (ok: boolean) => void,
  ): void {
    const task = this.getById(taskId);
    if (!task) {
      onDone?.(false);
      return;
    }

    const assigneeTouched = Object.prototype.hasOwnProperty.call(patch, 'assigneeId')
      || Object.prototype.hasOwnProperty.call(patch, 'assignee');

    const resolved = assigneeTouched
      ? patch.assigneeId == null && (patch.assignee == null || patch.assignee === '')
        ? { assignee: '', assigneeId: null as number | null }
        : this.resolveAssignee({
            assignee: patch.assignee ?? task.assignee,
            assigneeId: patch.assigneeId !== undefined ? patch.assigneeId : task.assigneeId,
          })
      : null;

    const appliedPatch = resolved
      ? { ...patch, assignee: resolved.assignee, assigneeId: resolved.assigneeId }
      : patch;

    const stageChanged = patch.stageId !== undefined && patch.stageId !== task.stageId;
    const stages = this.projectsStore.getStages(task.projectId);
    const nextStageName =
      patch.stageId != null ? (stages.find((s) => s.id === patch.stageId)?.name ?? null) : null;

    const updatedLocal = {
      ...task,
      ...appliedPatch,
      ...(stageChanged ? { stageName: nextStageName } : {}),
    };

    this._tasks.update((list) => list.map((t) => (t.id === taskId ? updatedLocal : t)));

    // Optimistic: recompute Stage A, Stage B, and project from all project tasks.
    this.refreshProjectProgress(task.projectId);
    this.employeesStore.syncFromTasks(this._tasks());
    this.employeesStore.syncFromProjects(this.projectsStore.projects());

    this.api
      .updateTask(taskId, {
        projectId: updatedLocal.projectId,
        stageId: updatedLocal.stageId,
        title: updatedLocal.title,
        description: updatedLocal.description ?? '',
        status: updatedLocal.status,
        priority: updatedLocal.priority,
        assigneeId: updatedLocal.assigneeId,
        startDate: updatedLocal.startDate || null,
        dueDate: updatedLocal.dueDate || null,
        estimatedHours: updatedLocal.estimatedHours ?? 1,
      })
      .subscribe({
        next: (updated) => {
          this._tasks.update((list) =>
            list.map((t) => (t.id === taskId ? this.normalizeTask(updated) : t)),
          );
          // Server recalculated Stage A + Stage B + project — reload stages and re-sync.
          this.refreshProjectProgress(updated.projectId);
          if (stageChanged) {
            this.projectsStore.reloadStages(updated.projectId);
          }
          onDone?.(true);
        },
        error: () => {
          // Roll back optimistic local change.
          this._tasks.update((list) => list.map((t) => (t.id === taskId ? task : t)));
          this.refreshProjectProgress(task.projectId);
          this.employeesStore.syncFromTasks(this._tasks());
          onDone?.(false);
        },
      });
  }

  /** Move task Stage A → Stage B; progress for A, B, and the project are recalculated. */
  moveTaskToStage(taskId: number, stageId: number | null): void {
    this.updateTask(taskId, { stageId });
  }

  /**
   * Soft-delete a task. Removed from Progress, on-time, delayed, KPI, and health;
   * project/stage progress recalculated immediately (local + server).
   */
  deleteTask(taskId: number): void {
    const task = this.getById(taskId);
    if (!task) return;
    const projectId = task.projectId;

    this._tasks.update((list) => list.filter((t) => t.id !== taskId));
    this.refreshProjectProgress(projectId);
    this.projectsStore.reloadStages(projectId);
    this.employeesStore.syncFromTasks(this._tasks());
    this.employeesStore.syncFromProjects(this.projectsStore.projects());

    this.api.deleteTask(taskId).subscribe({
      next: () => {
        this.refreshProjectProgress(projectId);
        this.projectsStore.reloadStages(projectId);
      },
      error: () => {
        // Reload tasks from server on failure would be ideal; keep optimistic removal for now.
      },
    });
  }

  private normalizeTask(task: TaskCard): TaskCard {
    return {
      ...task,
      startDate: task.startDate ? String(task.startDate).slice(0, 10) : '',
      dueDate: task.dueDate ? String(task.dueDate).slice(0, 10) : '',
      stageId: task.stageId ?? null,
      stageName: task.stageName ?? null,
      assigneeId: task.assigneeId ?? null,
      assignee: task.assignee ?? '',
      priority: task.priority ?? 'MEDIUM',
      status: task.status ?? 'TODO',
      estimatedHours: Number(task.estimatedHours) > 0 ? Number(task.estimatedHours) : 1,
      submittedForReviewAt: task.submittedForReviewAt ?? null,
      submittedForReviewById: task.submittedForReviewById ?? null,
      submittedForReviewBy: task.submittedForReviewBy ?? null,
      reviewReason: task.reviewReason ?? null,
    };
  }

}


