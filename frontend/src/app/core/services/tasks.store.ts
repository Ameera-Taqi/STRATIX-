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

  dueDate: string;

  status: TaskCard['status'];

  stageId?: number | null;

  description?: string;

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

    this.syncAllProjectProgress();

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

    return this._tasks().filter((t) => t.projectId === projectId);

  }



  getById(id: number): TaskCard | undefined {

    return this._tasks().find((t) => t.id === id);

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



    const body = {

      projectId: form.projectId,

      stageId: form.stageId ?? null,

      title: form.title.trim(),

      description: form.description ?? '',

      status: form.status,

      priority: form.priority,

      assigneeId,

      dueDate: form.dueDate || null,

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

      dueDate: form.dueDate,

      status: form.status,

      description: form.description,

    };



    this._tasks.update((list) => [...list, task]);

    this.notifications.push({
      titleKey: 'notifications.taskCreatedTitle',
      bodyKey: 'notifications.taskCreatedBody',
      params: { task: task.title, project: task.projectName },
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



  moveTask(taskId: number, newStatus: TaskCard['status']): void {

    const task = this.getById(taskId);

    if (!task || task.status === newStatus) return;

    this._tasks.update((list) =>

      list.map((t) => (t.id === taskId ? { ...t, status: newStatus } : t)),

    );

    this.notifications.push({

      titleKey: 'notifications.taskMovedTitle',

      bodyKey: 'notifications.taskMovedBody',

      params: { task: task.title, status: newStatus.replace('_', ' ') },

    });

    this.refreshProjectProgress(task.projectId);

    this.employeesStore.syncFromTasks(this._tasks());

    this.employeesStore.syncFromProjects(this.projectsStore.projects());



    this.api.updateTaskStatus(taskId, newStatus).subscribe({

      next: (updated) => {

        this._tasks.update((list) =>

          list.map((t) => (t.id === taskId ? this.normalizeTask(updated) : t)),

        );

      },

    });

  }



  updateTask(

    taskId: number,

    patch: Partial<Pick<TaskCard, 'title' | 'assignee' | 'assigneeId' | 'priority' | 'dueDate' | 'description'>>,

  ): void {

    const task = this.getById(taskId);

    if (!task) return;



    const resolved =

      patch.assigneeId != null || patch.assignee != null

        ? this.resolveAssignee({

            assignee: patch.assignee ?? task.assignee,

            assigneeId: patch.assigneeId ?? task.assigneeId,

          })

        : null;



    const appliedPatch = resolved

      ? { ...patch, assignee: resolved.assignee, assigneeId: resolved.assigneeId }

      : patch;



    const updatedLocal = { ...task, ...appliedPatch };

    this._tasks.update((list) =>

      list.map((t) => (t.id === taskId ? updatedLocal : t)),

    );

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

        dueDate: updatedLocal.dueDate || null,

      })

      .subscribe({

        next: (updated) => {

          this._tasks.update((list) =>

            list.map((t) => (t.id === taskId ? this.normalizeTask(updated) : t)),

          );

        },

      });

  }



  private normalizeTask(task: TaskCard): TaskCard {

    return {

      ...task,

      dueDate: task.dueDate ? String(task.dueDate).slice(0, 10) : '',

      stageId: task.stageId ?? null,

      stageName: task.stageName ?? null,

      assigneeId: task.assigneeId ?? null,

      assignee: task.assignee ?? '',

      priority: task.priority ?? 'MEDIUM',

      status: task.status ?? 'TODO',

    };

  }

}


