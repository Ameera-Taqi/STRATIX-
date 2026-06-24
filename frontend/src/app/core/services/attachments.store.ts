import { Injectable, inject, signal } from '@angular/core';
import { AuditLogStore } from './audit-log.store';
import { CurrentUserService } from './current-user.service';
import { NotificationsStore } from './notifications.store';

export interface Attachment {
  id: number;
  projectId: number;
  projectName: string;
  taskId?: number;
  fileName: string;
  fileSize: number;
  uploadedBy: string;
  uploadedAt: string;
}

@Injectable({ providedIn: 'root' })
export class AttachmentsStore {
  private readonly audit = inject(AuditLogStore);
  private readonly notifications = inject(NotificationsStore);
  private readonly currentUser = inject(CurrentUserService);
  private readonly _files = signal<Attachment[]>([]);
  private _nextId = 1;

  readonly files = this._files.asReadonly();

  forProject(projectId: number): Attachment[] {
    return this._files()
      .filter((f) => f.projectId === projectId)
      .sort((a, b) => b.uploadedAt.localeCompare(a.uploadedAt));
  }

  forTask(taskId: number): Attachment[] {
    return this._files()
      .filter((f) => f.taskId === taskId)
      .sort((a, b) => b.uploadedAt.localeCompare(a.uploadedAt));
  }

  upload(projectId: number, projectName: string, file: File, taskId?: number): Attachment {
    const uploadedBy = this.currentUser.profile().name;
    const attachment: Attachment = {
      id: this._nextId++,
      projectId,
      projectName,
      taskId,
      fileName: file.name,
      fileSize: file.size,
      uploadedBy,
      uploadedAt: new Date().toISOString(),
    };
    this._files.update((list) => [attachment, ...list]);
    this.audit.log({
      entityType: 'ATTACHMENT',
      entityId: attachment.id,
      entityLabel: file.name,
      action: 'UPLOAD',
      activityKey: 'FILE_UPLOADED',
      projectId,
      projectName,
      details: taskId ? `Uploaded to task #${taskId}` : 'Uploaded to project',
    });
    this.notifications.push({
      titleKey: 'notifications.uploadTitle',
      bodyKey: 'notifications.uploadBody',
      params: { file: file.name, project: projectName },
    });
    return attachment;
  }

  remove(id: number): void {
    const file = this._files().find((f) => f.id === id);
    if (!file) return;
    this._files.update((list) => list.filter((f) => f.id !== id));
    this.audit.log({
      entityType: 'ATTACHMENT',
      entityId: id,
      entityLabel: file.fileName,
      action: 'DELETE',
      projectId: file.projectId,
      projectName: file.projectName,
      details: 'File removed',
    });
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }
}
