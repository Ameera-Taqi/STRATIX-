import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { ProjectsStore } from '../../core/services/projects.store';
import { TasksStore } from '../../core/services/tasks.store';
import { EmployeesStore } from '../../core/services/employees.store';
import { TaskCard } from '../../core/data/mock-data';
import { StatusBadgeComponent } from '../../shared/components/status-badge.component';
import { TaskBoardComponent } from '../../shared/components/task-board/task-board.component';
import { GanttChartComponent } from '../../shared/components/gantt/gantt-chart.component';
import { GanttRow, GanttTaskMarker } from '../../shared/components/gantt/gantt.types';
import { ActivityTimelineComponent } from '../../shared/components/activity-timeline/activity-timeline.component';
import { AttachmentsPanelComponent } from '../../shared/components/attachments-panel/attachments-panel.component';
import { ProjectHealthScoreComponent } from '../../shared/components/project-health-score/project-health-score.component';
import { HealthBreakdownComponent } from '../../shared/components/health-breakdown/health-breakdown.component';
import { ProjectHealthService } from '../../core/services/project-health.service';
import { ProjectAiBotComponent } from '../../shared/components/project-ai-bot/project-ai-bot.component';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { UiIconComponent } from '../../shared/components/ui-icon/ui-icon.component';
import { CHART_COLORS, CHART_SERIES } from '../../shared/components/charts/chart-palette';

const STAGE_COLORS = [...CHART_SERIES];

export type WorkItemType = 'FEATURE' | 'TASK';

export interface WorkListItem {
  id: string;
  type: WorkItemType;
  entityId: number;
  title: string;
  status: string;
  assignee: string;
  dueDate: string;
  parentName: string | null;
}

@Component({
  selector: 'app-project-detail',
  standalone: true,
  imports: [
    TopbarComponent,
    StatusBadgeComponent,
    TaskBoardComponent,
    GanttChartComponent,
    ActivityTimelineComponent,
    AttachmentsPanelComponent,
    ProjectHealthScoreComponent,
    HealthBreakdownComponent,
    ProjectAiBotComponent,
    RouterLink,
    TranslatePipe,
    FormsModule,
    UiIconComponent,
  ],
  templateUrl: './project-detail.component.html',
})
export class ProjectDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly tasksStore = inject(TasksStore);
  private readonly employeesStore = inject(EmployeesStore);
  private readonly healthService = inject(ProjectHealthService);
  readonly store = inject(ProjectsStore);

  readonly projectId = signal(Number(this.route.snapshot.paramMap.get('id')));
  readonly employees = this.employeesStore.employees;

  readonly project = computed(() => {
    this.store.projects();
    return this.store.getById(this.projectId());
  });

  readonly stages = computed(() => {
    this.store.stagesByProject();
    return this.store.getStages(this.projectId());
  });

  readonly tasks = computed(() => {
    this.tasksStore.tasks();
    return this.tasksStore.getByProject(this.projectId());
  });

  readonly health = computed(() => {
    this.healthService.all();
    return this.healthService.getByProjectId(this.projectId());
  });

  /** Combined Feature + Task rows for List / Backlog. */
  readonly workItems = computed((): WorkListItem[] => {
    const features = this.stages().map(
      (s): WorkListItem => ({
        id: `F-${s.id}`,
        type: 'FEATURE',
        entityId: s.id,
        title: s.name,
        status: s.status,
        assignee: '—',
        dueDate: s.endDate,
        parentName: null,
      }),
    );
    const tasks = this.tasks().map(
      (t): WorkListItem => ({
        id: `T-${t.id}`,
        type: 'TASK',
        entityId: t.id,
        title: t.title,
        status: t.status,
        assignee: t.assignee || '—',
        dueDate: t.dueDate,
        parentName: t.stageName,
      }),
    );
    return [...features, ...tasks];
  });

  /** Backlog: open work only (not Done). */
  readonly backlogItems = computed(() =>
    this.workItems().filter((w) => {
      const s = w.status.toUpperCase().replace(/\s+/g, '_');
      return s !== 'DONE' && w.status !== 'Done';
    }),
  );

  readonly ganttRows = computed((): GanttRow[] =>
    this.stages().map((s, i) => ({
      id: s.id,
      label: s.name,
      start: s.startDate,
      end: s.endDate,
      progress: s.progress,
      color: STAGE_COLORS[i % STAGE_COLORS.length],
      sublabel: s.status,
    })),
  );

  readonly ganttMarkers = computed((): GanttTaskMarker[] => {
    this.tasksStore.tasks();
    return this.tasksStore
      .getByProject(this.projectId())
      .filter((t) => t.stageId != null)
      .map((t, i) => ({
        id: i + 1,
        rowId: t.stageId!,
        label: t.title,
        date: t.dueDate,
        color: t.status === 'DONE' ? CHART_COLORS.teal : CHART_COLORS.coral,
      }));
  });

  readonly stageError = signal<string | null>(null);
  readonly showStageForm = signal(false);
  readonly taskError = signal<string | null>(null);
  readonly showTaskForm = signal(false);
  readonly stageStatusOptions = ['Planned', 'Active', 'Done', 'On Hold'];

  stageForm = {
    name: '',
    startDate: '',
    endDate: '',
    status: 'Planned',
    progress: 0,
  };

  taskForm = {
    title: '',
    assigneeId: null as number | null,
    priority: 'MEDIUM' as TaskCard['priority'],
    dueDate: '',
    status: 'TODO' as TaskCard['status'],
    stageId: null as number | null,
  };

  tabKeys = [
    'project.tabs.overview',
    'project.tabs.timeline',
    'project.tabs.backlog',
    'project.tabs.board',
    'project.tabs.list',
    'project.tabs.files',
    'project.tabs.activity',
  ];
  activeTabKey = 'project.tabs.overview';

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const id = Number(params.get('id'));
      this.projectId.set(id);
      this.activeTabKey = 'project.tabs.overview';
      this.showStageForm.set(false);
      this.showTaskForm.set(false);
      this.store.ensureProject(id, () => this.tasksStore.loadFromApi());
      if (!this.employeesStore.loaded()) {
        this.employeesStore.loadFromApi();
      }
    });
  }

  setTab(key: string): void {
    this.activeTabKey = key;
  }

  workTypeLabel(type: WorkItemType): string {
    return type === 'FEATURE' ? 'work.type.feature' : 'work.type.task';
  }

  workTypeClass(type: WorkItemType): string {
    return type === 'FEATURE'
      ? 'bg-violet-100 text-violet-700 dark:bg-violet-900/40 dark:text-violet-300'
      : 'bg-sky-100 text-sky-700 dark:bg-sky-900/40 dark:text-sky-300';
  }

  statusPillClass(status: string): string {
    const s = status.toUpperCase().replace(/\s+/g, '_');
    if (s === 'DONE' || status === 'Done') {
      return 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-300';
    }
    if (s === 'IN_PROGRESS' || s === 'REVIEW' || status === 'Active') {
      return 'bg-sky-100 text-sky-800 dark:bg-sky-900/40 dark:text-sky-300';
    }
    return 'bg-slate-100 text-slate-700 dark:bg-slate-700 dark:text-slate-200';
  }

  openStageForm(): void {
    const p = this.project();
    if (!p) return;
    this.showTaskForm.set(false);
    this.stageError.set(null);
    this.stageForm = {
      name: '',
      startDate: p.startDate,
      endDate: p.endDate,
      status: 'Planned',
      progress: 0,
    };
    this.showStageForm.set(true);
  }

  cancelStageForm(): void {
    this.showStageForm.set(false);
    this.stageError.set(null);
  }

  saveStage(): void {
    const p = this.project();
    if (!p) return;
    if (!this.stageForm.name.trim()) {
      this.stageError.set('project.errorFeatureName');
      return;
    }
    if (!this.stageForm.endDate) {
      this.stageError.set('project.errorFeatureEnd');
      return;
    }
    this.store
      .addStage(this.projectId(), {
        name: this.stageForm.name,
        startDate: this.stageForm.startDate || p.startDate,
        endDate: this.stageForm.endDate,
        status: this.stageForm.status,
        progress: this.stageForm.progress,
      })
      .subscribe({
        next: () => {
          this.showStageForm.set(false);
          this.stageError.set(null);
        },
        error: () => this.stageError.set('project.errorFeatureSave'),
      });
  }

  openTaskForm(): void {
    const p = this.project();
    if (!p) return;
    this.showStageForm.set(false);
    this.taskError.set(null);
    const defaultAssignee = this.employees().find((e) => e.name === p.manager);
    this.taskForm = {
      title: '',
      assigneeId: defaultAssignee?.id ?? this.employees()[0]?.id ?? null,
      priority: 'MEDIUM',
      dueDate: p.endDate || new Date().toISOString().slice(0, 10),
      status: 'TODO',
      stageId: this.stages().length === 1 ? this.stages()[0].id : null,
    };
    this.showTaskForm.set(true);
  }

  cancelTaskForm(): void {
    this.showTaskForm.set(false);
    this.taskError.set(null);
  }

  saveTask(): void {
    if (!this.taskForm.title.trim()) {
      this.taskError.set('tasks.errorTitle');
      return;
    }
    if (this.taskForm.assigneeId == null) {
      this.taskError.set('tasks.errorAssignee');
      return;
    }
    const assignee = this.employees().find((e) => e.id === this.taskForm.assigneeId);
    this.tasksStore.addTask({
      projectId: this.projectId(),
      title: this.taskForm.title,
      assignee: assignee?.name ?? '',
      assigneeId: this.taskForm.assigneeId,
      priority: this.taskForm.priority,
      dueDate: this.taskForm.dueDate,
      status: this.taskForm.status,
      stageId: this.taskForm.stageId,
    });
    this.showTaskForm.set(false);
    this.taskError.set(null);
  }

  completeStage(id: number): void {
    this.store.completeStage(this.projectId(), id);
  }

  deleteStage(id: number): void {
    this.store.removeStage(this.projectId(), id);
  }

  taskLink(item: WorkListItem): string[] | null {
    return item.type === 'TASK' ? ['/tasks', String(item.entityId)] : null;
  }
}
