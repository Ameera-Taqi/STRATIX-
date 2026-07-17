import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import {
  ChangeRequestResponse,
  CreateChangeRequestRequest,
  UpdateChangeRequestRequest,
} from '../models/change-request.model';

@Injectable({ providedIn: 'root' })
export class ChangeRequestsStore {
  private readonly api = inject(ApiService);

  private readonly _changeRequests = signal<ChangeRequestResponse[]>([]);
  private readonly _loading = signal(false);
  private readonly _loadedProjects = signal<Set<number>>(new Set());

  readonly changeRequests = this._changeRequests.asReadonly();
  readonly loading = this._loading.asReadonly();

  forProject(projectId: number): ChangeRequestResponse[] {
    return this._changeRequests()
      .filter((c) => c.projectId === projectId)
      .sort((a, b) => b.createdAt.localeCompare(a.createdAt));
  }

  loadForProject(projectId: number): void {
    if (this._loadedProjects().has(projectId)) return;
    this._loading.set(true);
    this.api.getChangeRequests(projectId).subscribe({
      next: (list) => {
        this._changeRequests.update((current) => [
          ...current.filter((c) => c.projectId !== projectId),
          ...list,
        ]);
        this._loadedProjects.update((set) => new Set(set).add(projectId));
        this._loading.set(false);
      },
      error: () => this._loading.set(false),
    });
  }

  async create(body: CreateChangeRequestRequest): Promise<ChangeRequestResponse> {
    const created = await firstValueFrom(this.api.createChangeRequest(body));
    this._changeRequests.update((list) => [created, ...list]);
    return created;
  }

  async updateStatus(cr: ChangeRequestResponse, status: UpdateChangeRequestRequest['status'], reviewedById?: number): Promise<void> {
    const updated = await firstValueFrom(
      this.api.updateChangeRequest(cr.id, {
        title: cr.title,
        description: cr.description,
        status,
        priority: cr.priority as UpdateChangeRequestRequest['priority'],
        reviewedById: reviewedById ?? cr.reviewedById,
      }),
    );
    this._changeRequests.update((list) => list.map((c) => (c.id === cr.id ? updated : c)));
  }

  async remove(id: number): Promise<void> {
    await firstValueFrom(this.api.deleteChangeRequest(id));
    this._changeRequests.update((list) => list.filter((c) => c.id !== id));
  }
}
