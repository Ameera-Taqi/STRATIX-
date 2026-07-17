import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { CreateMilestoneRequest, MilestoneResponse, UpdateMilestoneRequest } from '../models/milestone.model';

@Injectable({ providedIn: 'root' })
export class MilestonesStore {
  private readonly api = inject(ApiService);

  private readonly _milestones = signal<MilestoneResponse[]>([]);
  private readonly _loading = signal(false);
  private readonly _loadedProjects = signal<Set<number>>(new Set());

  readonly milestones = this._milestones.asReadonly();
  readonly loading = this._loading.asReadonly();

  forProject(projectId: number): MilestoneResponse[] {
    return this._milestones()
      .filter((m) => m.projectId === projectId)
      .sort((a, b) => a.dueDate.localeCompare(b.dueDate));
  }

  loadForProject(projectId: number): void {
    if (this._loadedProjects().has(projectId)) return;
    this._loading.set(true);
    this.api.getMilestones(projectId).subscribe({
      next: (list) => {
        this._milestones.update((current) => [
          ...current.filter((m) => m.projectId !== projectId),
          ...list,
        ]);
        this._loadedProjects.update((set) => new Set(set).add(projectId));
        this._loading.set(false);
      },
      error: () => this._loading.set(false),
    });
  }

  async create(body: CreateMilestoneRequest): Promise<MilestoneResponse> {
    const created = await firstValueFrom(this.api.createMilestone(body));
    this._milestones.update((list) => [...list, created]);
    return created;
  }

  async update(id: number, body: UpdateMilestoneRequest): Promise<MilestoneResponse> {
    const updated = await firstValueFrom(this.api.updateMilestone(id, body));
    this._milestones.update((list) => list.map((m) => (m.id === id ? updated : m)));
    return updated;
  }

  async complete(milestone: MilestoneResponse): Promise<void> {
    await this.update(milestone.id, {
      projectId: milestone.projectId,
      title: milestone.title,
      dueDate: milestone.dueDate,
      completedDate: new Date().toISOString().slice(0, 10),
      status: 'COMPLETED',
    });
  }

  async remove(id: number): Promise<void> {
    await firstValueFrom(this.api.deleteMilestone(id));
    this._milestones.update((list) => list.filter((m) => m.id !== id));
  }
}
