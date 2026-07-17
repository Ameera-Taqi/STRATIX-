import { Injectable, inject, signal } from '@angular/core';
import { ApiService } from './api.service';
import { AuditLogStore } from './audit-log.store';
import { NotificationsStore } from './notifications.store';
import { ProjectFileResponse } from '../models/project-file.model';

export interface Attachment {
  id: number;
  projectId: number;
  projectName: string;
  fileName: string;
  fileSize: number;
  contentType: string | null;
  url: string;
  uploadedBy: string;
  uploadedAt: string;
}

function fromApi(f: ProjectFileResponse): Attachment {
  return {
    id: f.id,
    projectId: f.projectId,
    projectName: f.projectName,
    fileName: f.fileName,
    fileSize: f.sizeBytes,
    contentType: f.contentType,
    url: f.url,
    uploadedBy: f.uploadedByName,
    uploadedAt: f.createdAt,
  };
}

@Injectable({ providedIn: 'root' })
export class AttachmentsStore {
  private readonly api = inject(ApiService);
  private readonly audit = inject(AuditLogStore);
  private readonly notifications = inject(NotificationsStore);

  private readonly _files = signal<Attachment[]>([]);
  private readonly _loadedProjects = signal<Set<number>>(new Set());
  private readonly _loading = signal(false);

  readonly files = this._files.asReadonly();
  readonly loading = this._loading.asReadonly();

  loadForProject(projectId: number): void {
    if (this._loadedProjects().has(projectId)) return;
    this._loading.set(true);
    this.api.getProjectFiles(projectId).subscribe({
      next: (list) => {
        const mapped = list.map(fromApi);
        this._files.update((current) => [
          ...current.filter((f) => f.projectId !== projectId),
          ...mapped,
        ]);
        this._loadedProjects.update((set) => new Set(set).add(projectId));
        this._loading.set(false);
      },
      error: () => {
        this._loading.set(false);
      },
    });
  }

  forProject(projectId: number): Attachment[] {
    return this._files()
      .filter((f) => f.projectId === projectId)
      .sort((a, b) => b.uploadedAt.localeCompare(a.uploadedAt));
  }

  upload(projectId: number, projectName: string, file: File): void {
    const url = URL.createObjectURL(file);
    const body = {
      projectId,
      fileName: file.name,
      contentType: file.type || null,
      sizeBytes: file.size,
      url,
    };

    this.api.createProjectFile(body).subscribe({
      next: (created) => {
        const attachment = fromApi(created);
        this._files.update((list) => [attachment, ...list]);
        this.audit.log({
          entityType: 'ATTACHMENT',
          entityId: attachment.id,
          entityLabel: file.name,
          action: 'UPLOAD',
          activityKey: 'FILE_UPLOADED',
          projectId,
          projectName,
          details: 'Uploaded to project',
        });
        this.notifications.push({
          titleKey: 'notifications.uploadTitle',
          bodyKey: 'notifications.uploadBody',
          params: { file: file.name, project: projectName },
        });
      },
    });
  }

  remove(id: number): void {
    const file = this._files().find((f) => f.id === id);
    if (!file) return;
    this._files.update((list) => list.filter((f) => f.id !== id));
    this.api.deleteProjectFile(id).subscribe({
      next: () => {
        this.audit.log({
          entityType: 'ATTACHMENT',
          entityId: id,
          entityLabel: file.fileName,
          action: 'DELETE',
          projectId: file.projectId,
          projectName: file.projectName,
          details: 'File removed',
        });
      },
      error: () => {
        this._files.update((list) => [file, ...list]);
      },
    });
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }
}
