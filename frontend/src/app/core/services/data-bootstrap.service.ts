import { Injectable, inject } from '@angular/core';
import { EmployeesStore } from './employees.store';
import { NotificationsStore } from './notifications.store';
import { ProjectsStore } from './projects.store';
import { RisksStore } from './risks.store';
import { TasksStore } from './tasks.store';
import { CmsPermissionsStore } from './cms-permissions.store';
import { BrandingStore } from './branding.store';

@Injectable({ providedIn: 'root' })
export class DataBootstrapService {
  private readonly projectsStore = inject(ProjectsStore);
  private readonly tasksStore = inject(TasksStore);
  private readonly risksStore = inject(RisksStore);
  private readonly employeesStore = inject(EmployeesStore);
  private readonly notificationsStore = inject(NotificationsStore);
  private readonly cmsStore = inject(CmsPermissionsStore);
  private readonly brandingStore = inject(BrandingStore);

  bootstrap(): void {
    void this.cmsStore.load();
    void this.brandingStore.load();
    this.employeesStore.loadFromApi();
    this.projectsStore.loadFromApi(() => {
      this.tasksStore.loadFromApi();
    });
    this.risksStore.loadFromApi();
    this.notificationsStore.loadFromApi();
  }
}
