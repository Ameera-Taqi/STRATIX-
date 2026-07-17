import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { LanguageService } from '../../core/i18n/language.service';
import { BarChartComponent } from '../../shared/components/charts/bar-chart.component';
import { DonutChartComponent } from '../../shared/components/charts/donut-chart.component';
import { KpiCardComponent } from '../../shared/components/kpi-card.component';
import { ReportExportService } from '../../core/services/report-export.service';
import { ProjectsStore } from '../../core/services/projects.store';
import { TasksStore } from '../../core/services/tasks.store';
import { EmployeesStore } from '../../core/services/employees.store';
import { ProjectHealthService } from '../../core/services/project-health.service';
import { TaskCard } from '../../core/data/mock-data';
import { computeScheduleStatus } from '../../shared/utils/dashboard-insights.util';

type DateRangeFilter = 'all' | '7d' | '30d' | '90d';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [
    TopbarComponent,
    TranslatePipe,
    FormsModule,
    BarChartComponent,
    DonutChartComponent,
    KpiCardComponent,
  ],
  templateUrl: './reports.component.html',
})
export class ReportsComponent implements OnInit {
  private readonly exportService = inject(ReportExportService);
  private readonly lang = inject(LanguageService);
  private readonly projectsStore = inject(ProjectsStore);
  private readonly tasksStore = inject(TasksStore);
  private readonly employeesStore = inject(EmployeesStore);
  private readonly health = inject(ProjectHealthService);

  readonly dateRange = signal<DateRangeFilter>('all');
  readonly projectFilter = signal<number | 'all'>('all');
  readonly departmentFilter = signal<string>('all');
  readonly employeeFilter = signal<number | 'all'>('all');

  readonly dateRangeOptions: { value: DateRangeFilter; labelKey: string }[] = [
    { value: 'all', labelKey: 'reports.allTime' },
    { value: '7d', labelKey: 'reports.last7Days' },
    { value: '30d', labelKey: 'reports.last30Days' },
    { value: '90d', labelKey: 'reports.last90Days' },
  ];

  readonly projects = this.projectsStore.projects;
  readonly employees = this.employeesStore.employees;

  readonly departmentOptions = computed(() => {
    const names = [...new Set(this.projects().map((p) => p.department).filter(Boolean))];
    return names.sort();
  });

  readonly filteredProjects = computed(() => {
    this.projects();
    this.projectFilter();
    this.departmentFilter();

    let list = this.projectsStore.projects();
    const department = this.departmentFilter();
    if (department !== 'all') {
      list = list.filter((p) => p.department === department);
    }
    const projectId = this.projectFilter();
    if (projectId !== 'all') {
      list = list.filter((p) => p.id === projectId);
    }
    return list;
  });

  readonly filteredTasks = computed((): TaskCard[] => {
    this.tasksStore.tasks();
    this.dateRange();
    this.projectFilter();
    this.departmentFilter();
    this.employeeFilter();

    const projectIds = new Set(this.filteredProjects().map((p) => p.id));
    let list = this.tasksStore.getAll().filter((t) => projectIds.has(t.projectId));

    const employeeId = this.employeeFilter();
    if (employeeId !== 'all') {
      list = list.filter((t) => t.assigneeId === employeeId);
    }

    const range = this.dateRange();
    if (range !== 'all') {
      const days = range === '7d' ? 7 : range === '30d' ? 30 : 90;
      const to = new Date();
      const from = new Date();
      from.setDate(from.getDate() - days);
      const fromStr = from.toISOString().slice(0, 10);
      const toStr = to.toISOString().slice(0, 10);
      list = list.filter((t) => t.dueDate >= fromStr && t.dueDate <= toStr);
    }

    return list;
  });

  readonly filteredProjectIds = computed(() => new Set(this.filteredProjects().map((p) => p.id)));

  readonly summary = computed(() => {
    const projects = this.filteredProjects();
    const tasks = this.filteredTasks();
    const today = new Date().toISOString().slice(0, 10);
    const done = tasks.filter((t) => t.status === 'DONE').length;
    const overdue = tasks.filter((t) => t.dueDate < today && t.status !== 'DONE').length;
    const schedule = computeScheduleStatus(projects);
    const onTrack = schedule.filter((s) => s.status === 'ON_TRACK').length;
    const avgProgress =
      projects.length === 0
        ? 0
        : Math.round(projects.reduce((sum, p) => sum + p.progress, 0) / projects.length);

    return {
      projects: projects.length,
      avgProgress,
      overdue,
      onTrack,
      completionRate: tasks.length === 0 ? 0 : Math.round((done / tasks.length) * 100),
    };
  });

  readonly projectBars = computed(() =>
    this.health
      .all()
      .filter((p) => this.filteredProjectIds().has(p.projectId))
      .map((p) => ({
        label: this.shortLabel(p.projectName),
        value: p.progress,
        color: '#3b82f6',
      })),
  );

  readonly taskStatusDonut = computed(() => {
    this.lang.lang();
    const list = this.filteredTasks();
    const counts = { TODO: 0, IN_PROGRESS: 0, REVIEW: 0, DONE: 0 };
    for (const t of list) counts[t.status]++;
    return [
      { label: this.lang.t('task.todo'), value: counts.TODO, color: '#94a3b8' },
      { label: this.lang.t('task.inProgress'), value: counts.IN_PROGRESS, color: '#3b82f6' },
      { label: this.lang.t('task.review'), value: counts.REVIEW, color: '#f59e0b' },
      { label: this.lang.t('task.done'), value: counts.DONE, color: '#10b981' },
    ].filter((s) => s.value > 0);
  });

  readonly delayedBars = computed(() => {
    const today = new Date().toISOString().slice(0, 10);
    return this.filteredTasks()
      .filter((t) => t.dueDate < today && t.status !== 'DONE')
      .slice(0, 8)
      .map((t) => ({
        label: this.shortLabel(t.title, 18),
        value: Math.max(1, Math.floor((Date.parse(today) - Date.parse(t.dueDate)) / 86400000)),
        color: '#ef4444',
      }));
  });

  readonly scheduleStatus = computed(() => {
    const ids = this.filteredProjectIds();
    return computeScheduleStatus(this.filteredProjects().filter((p) => ids.has(p.id)));
  });

  readonly scheduleBars = computed(() =>
    this.scheduleStatus().map((b) => ({
      label: this.shortLabel(b.projectName),
      value: Math.round(b.spent / 1000),
      color: b.status === 'OVER_BUDGET' ? '#ef4444' : b.status === 'WARNING' ? '#f59e0b' : '#10b981',
      sublabel: `${Math.round(b.budget / 1000)}%`,
    })),
  );

  readonly donutCenter = computed(() => String(this.filteredTasks().length));

  readonly hasActiveFilters = computed(
    () =>
      this.dateRange() !== 'all' ||
      this.projectFilter() !== 'all' ||
      this.departmentFilter() !== 'all' ||
      this.employeeFilter() !== 'all',
  );

  clearFilters(): void {
    this.dateRange.set('all');
    this.projectFilter.set('all');
    this.departmentFilter.set('all');
    this.employeeFilter.set('all');
  }

  exportPdf(): void {
    this.exportService.exportExecutivePdf();
  }

  exportExcel(): void {
    const rows = [
      ['Project', 'Health', 'Progress %', 'Expected %', 'Schedule Status'],
      ...this.health
        .all()
        .filter((p) => this.filteredProjectIds().has(p.projectId))
        .map((p) => {
          const b = this.scheduleStatus().find((x) => x.projectId === p.projectId);
          return [
            p.projectName,
            p.score,
            p.progress,
            b ? Math.round(b.budget / 1000) : '',
            b?.status ?? '',
          ];
        }),
    ];
    const csv = rows.map((r) => r.map((c) => `"${String(c).replace(/"/g, '""')}"`).join(',')).join('\n');
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = 'stratix-executive-report.csv';
    a.click();
    URL.revokeObjectURL(a.href);
  }

  ngOnInit(): void {
    if (!this.projectsStore.loaded()) {
      this.projectsStore.loadFromApi(() => this.tasksStore.loadFromApi());
    }
    if (!this.employeesStore.loaded()) {
      this.employeesStore.loadFromApi();
    }
  }

  private shortLabel(text: string, max = 14): string {
    const trimmed = text.trim();
    if (trimmed.length <= max) return trimmed;
    return `${trimmed.slice(0, max - 1)}…`;
  }
}
