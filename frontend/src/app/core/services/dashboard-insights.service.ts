import { Injectable, computed, inject } from '@angular/core';
import { ProjectsStore } from './projects.store';
import { TasksStore } from './tasks.store';
import { EmployeesStore } from './employees.store';
import {
  computeDepartmentPerformance,
  computeEmployeeLoad,
  computeOverdueTasks,
  computeScheduleStatus,
} from '../../shared/utils/dashboard-insights.util';

@Injectable({ providedIn: 'root' })
export class DashboardInsightsService {
  private readonly projectsStore = inject(ProjectsStore);
  private readonly tasksStore = inject(TasksStore);
  private readonly employeesStore = inject(EmployeesStore);

  readonly overdueTasks = computed(() => {
    this.tasksStore.tasks();
    return computeOverdueTasks(this.tasksStore.getAll());
  });

  readonly employeeLoad = computed(() => {
    this.tasksStore.tasks();
    this.employeesStore.employees();
    return computeEmployeeLoad(this.employeesStore.getAll(), this.tasksStore.getAll());
  });

  readonly departmentPerformance = computed(() => {
    this.projectsStore.projects();
    this.tasksStore.tasks();
    return computeDepartmentPerformance(
      this.projectsStore.projects(),
      this.tasksStore.getAll(),
    );
  });

  readonly scheduleStatus = computed(() => {
    this.projectsStore.projects();
    return computeScheduleStatus(this.projectsStore.projects());
  });
}
