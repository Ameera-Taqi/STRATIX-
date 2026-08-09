import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { RoleAccessService } from './role-access.service';
import { ProjectsStore } from './projects.store';
import { QuickCreateAction, QuickCreateItem, QUICK_CREATE_ITEMS } from '../config/quick-create';

export type ProjectScopedCreate = 'feature' | 'task';

@Injectable({ providedIn: 'root' })
export class QuickCreateService {
  private readonly roleAccess = inject(RoleAccessService);
  private readonly projects = inject(ProjectsStore);
  private readonly router = inject(Router);

  /** Actions the current user may create — empty hides the + Create button. */
  readonly items = computed((): QuickCreateItem[] =>
    QUICK_CREATE_ITEMS.filter((item) => this.roleAccess.canWrite(item.module)),
  );

  readonly visible = computed(() => this.items().length > 0);

  /** Project picker dialog (Feature / Task when not already in a workspace). */
  readonly pickerOpen = signal(false);
  readonly pickerKind = signal<ProjectScopedCreate | null>(null);
  readonly pickerProjectId = signal<number | null>(null);
  readonly pickerError = signal<string | null>(null);
  readonly pickerLoading = signal(false);

  readonly pickerProjects = computed(() => this.projects.projects());

  readonly pickerTitleKey = computed(() => {
    const kind = this.pickerKind();
    if (kind === 'feature') return 'quickCreate.pickFeatureTitle';
    if (kind === 'task') return 'quickCreate.pickTaskTitle';
    return 'quickCreate.pickProjectTitle';
  });

  async run(action: QuickCreateAction): Promise<void> {
    switch (action) {
      case 'project':
        await this.router.navigate(['/projects'], { queryParams: { create: '1' } });
        return;
      case 'feature':
        await this.openProjectScoped('feature');
        return;
      case 'task':
        await this.openProjectScoped('task');
        return;
      case 'risk':
        await this.router.navigate(['/risks'], { queryParams: { create: '1' } });
        return;
      case 'user':
        await this.router.navigate(['/team'], { queryParams: { create: '1' } });
        return;
      case 'report':
        await this.router.navigate(['/reports']);
        return;
    }
  }

  closePicker(): void {
    this.pickerOpen.set(false);
    this.pickerKind.set(null);
    this.pickerProjectId.set(null);
    this.pickerError.set(null);
    this.pickerLoading.set(false);
  }

  async confirmPicker(): Promise<void> {
    const kind = this.pickerKind();
    const projectId = this.pickerProjectId();
    if (!kind) {
      this.closePicker();
      return;
    }
    if (projectId == null) {
      this.pickerError.set('quickCreate.errorSelectProject');
      return;
    }
    this.pickerError.set(null);
    this.closePicker();
    await this.navigateToCreate(projectId, kind);
  }

  private async openProjectScoped(kind: ProjectScopedCreate): Promise<void> {
    const currentId = this.currentProjectIdFromRoute();
    if (currentId != null) {
      await this.navigateToCreate(currentId, kind);
      return;
    }

    this.pickerLoading.set(true);
    this.pickerError.set(null);
    await this.ensureProjectsLoaded();
    this.pickerLoading.set(false);

    const list = this.projects.projects();
    if (list.length === 0) {
      await this.router.navigate(['/projects'], { queryParams: { create: '1' } });
      return;
    }
    if (list.length === 1) {
      await this.navigateToCreate(list[0].id, kind);
      return;
    }

    this.pickerKind.set(kind);
    this.pickerProjectId.set(null);
    this.pickerOpen.set(true);
  }

  private async navigateToCreate(projectId: number, kind: ProjectScopedCreate): Promise<void> {
    const queryParams =
      kind === 'feature'
        ? { createFeature: '1', tab: 'features' }
        : { createTask: '1', tab: 'tasks' };
    await this.router.navigate(['/projects', projectId], { queryParams });
  }

  /** When already inside `/projects/:id`, use that project — never invent one. */
  private currentProjectIdFromRoute(): number | null {
    const match = this.router.url.match(/\/projects\/(\d+)(?:[/?#]|$)/);
    if (!match) return null;
    const id = Number(match[1]);
    return Number.isFinite(id) && id > 0 ? id : null;
  }

  private ensureProjectsLoaded(): Promise<void> {
    if (this.projects.loaded()) return Promise.resolve();
    return new Promise((resolve) => {
      this.projects.loadFromApi(() => resolve());
    });
  }
}
