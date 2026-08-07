import { Injectable, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { RoleAccessService } from './role-access.service';
import { ProjectsStore } from './projects.store';
import { QuickCreateAction, QuickCreateItem, QUICK_CREATE_ITEMS } from '../config/quick-create';

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

  async run(action: QuickCreateAction): Promise<void> {
    switch (action) {
      case 'project':
        await this.router.navigate(['/projects'], { queryParams: { create: '1' } });
        return;
      case 'feature':
        await this.openOnProject({ createFeature: '1', tab: 'features' });
        return;
      case 'task':
        await this.openOnProject({ createTask: '1', tab: 'tasks' });
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

  private async openOnProject(queryParams: Record<string, string>): Promise<void> {
    await this.ensureProjectsLoaded();
    const target = this.projects.projects()[0];
    if (!target) {
      await this.router.navigate(['/projects'], { queryParams: { create: '1' } });
      return;
    }
    await this.router.navigate(['/projects', target.id], { queryParams });
  }

  private ensureProjectsLoaded(): Promise<void> {
    if (this.projects.loaded()) return Promise.resolve();
    return new Promise((resolve) => {
      this.projects.loadFromApi(() => resolve());
    });
  }
}
