import { Injectable, inject } from '@angular/core';
import { EmployeesStore } from './employees.store';
import { ProjectsStore } from './projects.store';
import { RisksStore } from './risks.store';
import { TasksStore } from './tasks.store';

@Injectable({ providedIn: 'root' })
export class DataBootstrapService {
  private readonly projectsStore = inject(ProjectsStore);
  private readonly tasksStore = inject(TasksStore);
  private readonly risksStore = inject(RisksStore);
  private readonly employeesStore = inject(EmployeesStore);

  bootstrap(): void {
    this.employeesStore.loadFromApi();
    this.projectsStore.loadFromApi(() => {
      this.tasksStore.loadFromApi();
    });
    this.risksStore.loadFromApi();
  }
}
