import { Injectable, inject, signal } from '@angular/core';
import { AuditLogStore } from './audit-log.store';
import { CurrentUserService } from './current-user.service';
import { NotificationsStore } from './notifications.store';

export interface TaskComment {
  id: number;
  taskId: number;
  taskTitle: string;
  author: string;
  text: string;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class CommentsStore {
  private readonly audit = inject(AuditLogStore);
  private readonly notifications = inject(NotificationsStore);
  private readonly currentUser = inject(CurrentUserService);
  private readonly _comments = signal<TaskComment[]>([]);
  private _nextId = 1;

  readonly comments = this._comments.asReadonly();

  forTask(taskId: number): TaskComment[] {
    return this._comments()
      .filter((c) => c.taskId === taskId)
      .sort((a, b) => b.createdAt.localeCompare(a.createdAt));
  }

  add(
    taskId: number,
    taskTitle: string,
    text: string,
    projectId?: number,
    projectName?: string,
  ): TaskComment {
    const author = this.currentUser.profile().name;
    const comment: TaskComment = {
      id: this._nextId++,
      taskId,
      taskTitle,
      author,
      text: text.trim(),
      createdAt: new Date().toISOString(),
    };
    this._comments.update((list) => [comment, ...list]);
    this.audit.log({
      entityType: 'COMMENT',
      entityId: comment.id,
      entityLabel: taskTitle,
      action: 'COMMENT',
      activityKey: 'COMMENT_ADDED',
      projectId,
      projectName,
      details: text.trim(),
    });
    this.notifications.push({
      titleKey: 'notifications.commentTitle',
      bodyKey: 'notifications.commentBody',
      params: { task: taskTitle, author },
    });
    return comment;
  }
}
