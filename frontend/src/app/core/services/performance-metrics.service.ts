import { Injectable, computed, inject, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { ApiService } from './api.service';
import { EmployeesStore } from './employees.store';
import { ProjectsStore } from './projects.store';
import { RisksStore } from './risks.store';
import { TasksStore } from './tasks.store';
import {
  computePerformanceMetrics,
  PerformanceMetrics,
} from '../../shared/utils/performance-metrics.util';

const EMPTY_METRICS: PerformanceMetrics = {
  completionRate: 0,
  onTimePct: 100,
  overdueTasks: 0,
  onTimeTaskCount: 0,
  teamProductivity: 0,
  avgTaskCompletion: 0,
  riskImpact: 0,
  totalTasks: 0,
  completedTasks: 0,
  openCriticalRisks: 0,
  hasData: false,
};

@Injectable({ providedIn: 'root' })
export class PerformanceMetricsService {
  private readonly api = inject(ApiService);
  private readonly tasksStore = inject(TasksStore);
  private readonly employeesStore = inject(EmployeesStore);
  private readonly projectsStore = inject(ProjectsStore);
  private readonly risksStore = inject(RisksStore);

  private readonly _reloadError = signal<string | null>(null);

  readonly error = this._reloadError.asReadonly();

  readonly loading = computed(
    () =>
      !this.tasksStore.loaded() ||
      !this.employeesStore.loaded() ||
      !this.projectsStore.loaded(),
  );

  readonly metrics = computed((): PerformanceMetrics => {
    this.tasksStore.tasks();
    this.employeesStore.employees();
    this.risksStore.risks();
    this.projectsStore.projects();

    if (this.loading()) return EMPTY_METRICS;

    return computePerformanceMetrics(
      this.tasksStore.getAll(),
      this.employeesStore.getAll(),
      this.risksStore.risks(),
    );
  });

  ensureData(): void {
    this._reloadError.set(null);

    if (!this.employeesStore.loaded()) {
      this.employeesStore.loadFromApi();
    }

    if (!this.projectsStore.loaded()) {
      this.projectsStore.loadFromApi(() => {
        if (!this.tasksStore.loaded()) {
          this.tasksStore.loadFromApi();
        }
      });
    } else if (!this.tasksStore.loaded()) {
      this.tasksStore.loadFromApi();
    }

    if (!this.risksStore.loaded()) {
      this.risksStore.loadFromApi();
    }
  }

  reload(): void {
    this._reloadError.set(null);

    forkJoin({
      health: this.api.health(),
      users: this.api.getDirectoryUsers(),
      projects: this.api.getProjects(),
      tasks: this.api.getTasks(),
      risks: this.api.getRisks(),
    }).subscribe({
      next: () => {
        this.tasksStore.loadFromApi();
        this.employeesStore.loadFromApi();
        this.projectsStore.loadFromApi(() => this.tasksStore.loadFromApi());
        this.risksStore.loadFromApi();
      },
      error: () => this._reloadError.set('performance.errorLoad'),
    });
  }
}
