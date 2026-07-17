import { Injectable, inject, signal } from '@angular/core';
import { ApiService } from './api.service';
import { AuditLogStore } from './audit-log.store';
import { NotificationsStore } from './notifications.store';
import { TaskCommentResponse } from '../models/task-comment.model';

export interface TaskComment {
  id: number;
  taskId: number;
  author: string;
  text: string;
  createdAt: string;
}

function fromApi(c: TaskCommentResponse): TaskComment {
  return {
    id: c.id,
    taskId: c.taskId,
    author: c.userName,
    text: c.comment,
    createdAt: c.createdAt,
  };
}

@Injectable({ providedIn: 'root' })
export class CommentsStore {
  private readonly api = inject(ApiService);
  private readonly audit = inject(AuditLogStore);
  private readonly notifications = inject(NotificationsStore);

  private readonly _comments = signal<TaskComment[]>([]);
  private readonly _loadedTasks = signal<Set<number>>(new Set());
  private readonly _loading = signal(false);

  readonly comments = this._comments.asReadonly();
  readonly loading = this._loading.asReadonly();

  loadForTask(taskId: number): void {
    if (this._loadedTasks().has(taskId)) return;
    this._loading.set(true);
    this.api.getTaskComments(taskId).subscribe({
      next: (list) => {
        const mapped = list.map(fromApi);
        this._comments.update((current) => [
          ...current.filter((c) => c.taskId !== taskId),
          ...mapped,
        ]);
        this._loadedTasks.update((set) => new Set(set).add(taskId));
        this._loading.set(false);
      },
      error: () => {
        this._loading.set(false);
      },
    });
  }

  forTask(taskId: number): TaskComment[] {
    return this._comments()
      .filter((c) => c.taskId === taskId)
      .sort((a, b) => b.createdAt.localeCompare(a.createdAt));
  }

  add(taskId: number, taskTitle: string, text: string, projectId?: number, projectName?: string): void {
    const comment = text.trim();
    this.api.createTaskComment({ taskId, comment }).subscribe({
      next: (created) => {
        const mapped = fromApi(created);
        this._comments.update((list) => [mapped, ...list]);
        this.audit.log({
          entityType: 'COMMENT',
          entityId: mapped.id,
          entityLabel: taskTitle,
          action: 'COMMENT',
          activityKey: 'COMMENT_ADDED',
          projectId,
          projectName,
          details: comment,
        });
        this.notifications.push({
          titleKey: 'notifications.commentTitle',
          bodyKey: 'notifications.commentBody',
          params: { task: taskTitle, author: mapped.author },
        });
      },
    });
  }

  remove(id: number): void {
    const comment = this._comments().find((c) => c.id === id);
    if (!comment) return;
    this._comments.update((list) => list.filter((c) => c.id !== id));
    this.api.deleteTaskComment(id).subscribe({
      error: () => {
        this._comments.update((list) => [comment, ...list]);
      },
    });
  }
}
