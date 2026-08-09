import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { ProjectsStore } from '../../core/services/projects.store';
import { TasksStore } from '../../core/services/tasks.store';
import { EmployeesStore } from '../../core/services/employees.store';
import { RisksStore } from '../../core/services/risks.store';
import { TaskCard } from '../../core/data/mock-data';
import { StatusBadgeComponent } from '../../shared/components/status-badge.component';
import { TaskBoardComponent } from '../../shared/components/task-board/task-board.component';
import { GanttChartComponent } from '../../shared/components/gantt/gantt-chart.component';
import { GanttRow, GanttTaskMarker } from '../../shared/components/gantt/gantt.types';
import { ProjectTimelineComponent } from '../../shared/components/project-timeline/project-timeline.component';
import { ActivityTimelineComponent } from '../../shared/components/activity-timeline/activity-timeline.component';
import { AttachmentsPanelComponent } from '../../shared/components/attachments-panel/attachments-panel.component';
import { ProjectHealthService } from '../../core/services/project-health.service';
import { ProjectAiBotComponent } from '../../shared/components/project-ai-bot/project-ai-bot.component';
import { ProjectHealthOverviewComponent } from '../../shared/components/project-health-overview/project-health-overview.component';
import {
  BreadcrumbItem,
  BreadcrumbsComponent,
} from '../../shared/components/breadcrumbs/breadcrumbs.component';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { UiIconComponent } from '../../shared/components/ui-icon/ui-icon.component';
import { CHART_COLORS, CHART_SERIES } from '../../shared/components/charts/chart-palette';
import { healthStatusClass } from '../../shared/utils/project-health.util';
import {
  calculateRiskLevel,
  countRisksByLevel,
  riskLevelClass,
  riskStatusClass,
} from '../../shared/utils/risk.util';
import {
  priorityLabelKey,
  riskImpactLabelKey,
  riskLevelLabelKey,
  riskProbabilityLabelKey,
  riskStatusLabelKey,
} from '../../shared/utils/enum-labels';
import { effortBreakdownFromTasks } from '../../shared/utils/project-progress.util';
import {
  CreateRiskForm,
  RiskImpact,
  RiskProbability,
  RiskStatus,
} from '../../core/models/risk.model';
import { CurrentUserService } from '../../core/services/current-user.service';
import { LanguageService } from '../../core/i18n/language.service';
import { WarnUnsavedDirective } from '../../shared/directives/warn-unsaved.directive';
import {
  allowLeaveIfClean,
  formSnapshot,
  HasUnsavedChanges,
  isFormDirty,
} from '../../core/unsaved/unsaved-changes';

@Component({
  selector: 'app-project-detail',
  standalone: true,
  imports: [
    TopbarComponent,
    StatusBadgeComponent,
    TaskBoardComponent,
    GanttChartComponent,
    ProjectTimelineComponent,
    ActivityTimelineComponent,
    AttachmentsPanelComponent,
    ProjectAiBotComponent,
    ProjectHealthOverviewComponent,
    BreadcrumbsComponent,
    RouterLink,
    TranslatePipe,
    FormsModule,
    UiIconComponent,
    WarnUnsavedDirective,
  ],
  templateUrl: './project-detail.component.html',
  styles: `
    :host {
      display: flex;
      flex: 1 1 auto;
      flex-direction: column;
      min-height: 0;
      overflow: hidden;
    }
  `,
})
export class ProjectDetailComponent implements OnInit, HasUnsavedChanges {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly tasksStore = inject(TasksStore);
  private readonly employeesStore = inject(EmployeesStore);
  private readonly lang = inject(LanguageService);
  private stageFormBaseline: string | null = null;
  private taskFormBaseline: string | null = null;
  private riskFormBaseline: string | null = null;
  private readonly risksStore = inject(RisksStore);
  private readonly healthService = inject(ProjectHealthService);
  private readonly currentUser = inject(CurrentUserService);
  readonly store = inject(ProjectsStore);

  readonly projectId = signal(Number(this.route.snapshot.paramMap.get('id')));
  readonly employees = this.employeesStore.employees;
  readonly healthStatusClass = healthStatusClass;
  readonly riskLevelClass = riskLevelClass;
  readonly riskStatusClass = riskStatusClass;
  readonly priorityLabelKeyFn = priorityLabelKey;
  readonly riskLevelKey = riskLevelLabelKey;
  readonly riskStatusKey = riskStatusLabelKey;
  readonly riskImpactKey = riskImpactLabelKey;
  readonly riskProbabilityKey = riskProbabilityLabelKey;

  readonly impactOptions: RiskImpact[] = ['LOW', 'MEDIUM', 'HIGH'];
  readonly probabilityOptions: RiskProbability[] = ['LOW', 'MEDIUM', 'HIGH'];
  /** Create/edit active statuses only — closing uses Close Risk modal. */
  readonly riskStatusOptions: RiskStatus[] = ['OPEN', 'MITIGATING'];
  readonly residualOptions: Array<RiskImpact | null> = [null, 'LOW', 'MEDIUM', 'HIGH'];

  readonly showCloseRiskModal = signal(false);
  readonly closingRiskId = signal<number | null>(null);
  readonly closeRiskError = signal<string | null>(null);
  closeRiskForm = {
    closureReason: '',
    residualRisk: null as RiskImpact | null,
  };

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

  readonly projectRisks = computed(() => {
    this.risksStore.risks();
    return this.risksStore.getByProject(this.projectId());
  });

  readonly openRiskCount = computed(
    () => this.projectRisks().filter((r) => r.status !== 'CLOSED').length,
  );

  readonly projectRiskLevelCounts = computed(() => countRisksByLevel(this.projectRisks()));

  readonly showRiskForm = signal(false);
  readonly riskFormError = signal<string | null>(null);
  riskForm: CreateRiskForm = {
    title: '',
    description: '',
    impact: 'MEDIUM',
    probability: 'MEDIUM',
    mitigationPlan: '',
    status: 'OPEN',
    projectId: 0,
    ownerId: 0,
  };

  readonly health = computed(() => {
    this.healthService.all();
    return this.healthService.getByProjectId(this.projectId());
  });

  /** Read-only project progress from task effort (never manually editable). */
  readonly projectProgress = computed(() => effortBreakdownFromTasks(this.tasks()));

  readonly showProgressInfo = signal(false);

  /** Feature = functional slice of the project; cards summarize nested tasks. */
  readonly featureCards = computed(() => {
    const today = new Date().toISOString().slice(0, 10);
    const tasks = this.tasks();
    return this.stages().map((stage, index, all) => {
      const stageTasks = tasks.filter((t) => Number(t.stageId) === Number(stage.id));
      const breakdown = effortBreakdownFromTasks(stageTasks);
      const overdue = stageTasks.filter(
        (t) => !!t.dueDate && t.dueDate < today && t.status !== 'DONE',
      ).length;
      const blocked = stageTasks.filter((t) => t.status === 'BLOCKED').length;
      return {
        stage,
        progress: breakdown.progress,
        effortTotal: breakdown.effortTotal,
        effortDone: breakdown.effortDone,
        tasksDone: stageTasks.filter((t) => t.status === 'DONE').length,
        tasksTotal: breakdown.taskCount,
        overdue,
        blocked,
        dueLabel: this.formatDue(stage.endDate),
        canMoveUp: index > 0,
        canMoveDown: index < all.length - 1,
        tasks: stageTasks,
      };
    });
  });

  readonly openedFeatureId = signal<number | null>(null);

  readonly openedFeature = computed(() => {
    const id = this.openedFeatureId();
    if (id == null) return null;
    return this.featureCards().find((c) => c.stage.id === id) ?? null;
  });

  readonly breadcrumbs = computed((): BreadcrumbItem[] => {
    const p = this.project();
    if (!p) {
      return [{ labelKey: 'nav.projects', link: '/projects' }];
    }
    const feature = this.openedFeature();
    const crumbs: BreadcrumbItem[] = [
      { labelKey: 'nav.projects', link: '/projects' },
      feature
        ? { label: p.name, link: ['/projects', p.id], queryParams: { clearFeature: '1' } }
        : { label: p.name },
    ];
    if (feature) {
      crumbs.push({ label: feature.stage.name });
    }
    return crumbs;
  });

  readonly stageError = signal<string | null>(null);
  readonly showStageForm = signal(false);
  readonly savingFeature = signal(false);
  readonly createdFeature = signal<{ id: number; name: string } | null>(null);
  readonly taskError = signal<string | null>(null);
  readonly showTaskForm = signal(false);

  stageForm = {
    name: '',
    description: '',
    startDate: '',
    endDate: '',
  };

  taskForm = {
    title: '',
    description: '',
    assigneeId: null as number | null,
    priority: 'MEDIUM' as TaskCard['priority'],
    estimatedHours: 1,
    startDate: '',
    dueDate: '',
    status: 'TODO' as TaskCard['status'],
    stageId: null as number | null,
  };

  readonly ganttRows = computed((): GanttRow[] => {
    const p = this.project();
    const fallbackStart = p?.startDate || new Date().toISOString().slice(0, 10);
    const fallbackEnd = p?.endDate || fallbackStart;
    return this.stages().map((s, i) => {
      const start = s.startDate || fallbackStart;
      const end = s.endDate || s.startDate || fallbackEnd;
      return {
        id: s.id,
        label: s.name,
        start,
        end: end < start ? start : end,
        progress: Number(s.progress) || 0,
        color: CHART_SERIES[i % CHART_SERIES.length],
        sublabel: s.status || '',
      };
    });
  });

  readonly ganttMarkers = computed((): GanttTaskMarker[] =>
    this.tasks()
      .filter((t) => t.stageId != null && !!t.dueDate)
      .map((t, i) => ({
        id: i + 1,
        rowId: t.stageId!,
        label: t.title,
        date: t.dueDate,
        color: t.status === 'DONE' ? CHART_COLORS.teal : CHART_COLORS.coral,
      })),
  );

  readonly timelineFeatures = computed(() => {
    const p = this.project();
    const fallbackStart = p?.startDate || new Date().toISOString().slice(0, 10);
    const fallbackEnd = p?.endDate || fallbackStart;
    return this.stages().map((s) => {
      const startDate = s.startDate || fallbackStart;
      const endDate = s.endDate || s.startDate || fallbackEnd;
      return {
        id: s.id,
        name: s.name,
        startDate,
        endDate: endDate < startDate ? startDate : endDate,
        progress: s.progress,
        status: s.status,
      };
    });
  });

  readonly timelineTasks = computed(() =>
    this.tasks().map((t) => ({
      id: t.id,
      title: t.title,
      stageId: t.stageId,
      startDate: t.startDate,
      dueDate: t.dueDate,
      status: t.status,
    })),
  );

  readonly timelineLocale = computed(() => (this.lang.lang() === 'ar' ? 'ar' : 'en'));

  /** Unified workspace tabs — everything for one project lives here. */
  readonly tabKeys = [
    'project.tabs.overview',
    'project.tabs.features',
    'project.tabs.tasks',
    'project.tabs.timeline',
    'project.tabs.gantt',
    'project.tabs.risks',
    'project.tabs.files',
    'project.tabs.activity',
  ] as const;

  activeTabKey: string = 'project.tabs.overview';

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const id = Number(params.get('id'));
      if (id !== this.projectId() && this.hasUnsavedChanges()) {
        if (!allowLeaveIfClean(true, this.lang)) {
          void this.router.navigate(['/projects', this.projectId()], { replaceUrl: true });
          return;
        }
      }
      this.projectId.set(id);
      this.activeTabKey = 'project.tabs.overview';
      this.openedFeatureId.set(null);
      this.createdFeature.set(null);
      this.showStageForm.set(false);
      this.showTaskForm.set(false);
      this.showRiskForm.set(false);
      this.stageFormBaseline = null;
      this.taskFormBaseline = null;
      this.riskFormBaseline = null;
      this.store.ensureProject(id, () => this.tasksStore.loadFromApi());
      this.risksStore.loadFromApi();
      if (!this.employeesStore.loaded()) {
        this.employeesStore.loadFromApi();
      }
    });

    this.route.queryParamMap.subscribe((q) => {
      const tab = q.get('tab');
      const map: Record<string, string> = {
        overview: 'project.tabs.overview',
        features: 'project.tabs.features',
        tasks: 'project.tabs.tasks',
        timeline: 'project.tabs.timeline',
        gantt: 'project.tabs.gantt',
        risks: 'project.tabs.risks',
        files: 'project.tabs.files',
        activity: 'project.tabs.activity',
      };
      if (tab && map[tab]) this.activeTabKey = map[tab];

      const openFeature = q.get('createFeature') === '1';
      const openTask = q.get('createTask') === '1';
      const featureIdRaw = q.get('featureId');
      const featureId = featureIdRaw ? Number(featureIdRaw) : null;

      if (q.get('clearFeature') === '1') {
        this.openedFeatureId.set(null);
        void this.router.navigate([], {
          relativeTo: this.route,
          queryParams: { clearFeature: null },
          queryParamsHandling: 'merge',
          replaceUrl: true,
        });
      }

      if (featureId && !Number.isNaN(featureId)) {
        this.activeTabKey = 'project.tabs.features';
        this.openedFeatureId.set(featureId);
        void this.router.navigate([], {
          relativeTo: this.route,
          queryParams: { featureId: null },
          queryParamsHandling: 'merge',
          replaceUrl: true,
        });
      }

      if (!openFeature && !openTask) return;

      // Clear params so refresh does not reopen the form.
      void this.router.navigate([], {
        relativeTo: this.route,
        queryParams: { createFeature: null, createTask: null },
        queryParamsHandling: 'merge',
        replaceUrl: true,
      });

      const open = () => {
        if (openFeature) {
          this.activeTabKey = 'project.tabs.features';
          this.openStageForm();
        } else if (openTask) {
          this.activeTabKey = 'project.tabs.tasks';
          this.openTaskForm();
        }
      };

      if (this.project()) {
        open();
      } else {
        this.store.ensureProject(this.projectId(), () => open());
      }
    });
  }

  setTab(key: string): void {
    this.activeTabKey = key;
  }

  hasUnsavedChanges(): boolean {
    if (this.showStageForm() && isFormDirty(this.stageForm, this.stageFormBaseline)) return true;
    if (this.showTaskForm() && isFormDirty(this.taskForm, this.taskFormBaseline)) return true;
    if (this.showRiskForm() && isFormDirty(this.riskForm, this.riskFormBaseline)) return true;
    return false;
  }

  openStageForm(): void {
    const p = this.project();
    if (!p) return;
    if (
      !allowLeaveIfClean(
        this.showTaskForm() && isFormDirty(this.taskForm, this.taskFormBaseline),
        this.lang,
      )
    ) {
      return;
    }
    this.showTaskForm.set(false);
    this.taskFormBaseline = null;
    this.createdFeature.set(null);
    this.stageError.set(null);
    this.stageForm = {
      name: '',
      description: '',
      startDate: p.startDate || new Date().toISOString().slice(0, 10),
      endDate: '',
    };
    this.stageFormBaseline = formSnapshot(this.stageForm);
    this.showStageForm.set(true);
  }

  cancelStageForm(): void {
    if (!allowLeaveIfClean(
      this.showStageForm() && isFormDirty(this.stageForm, this.stageFormBaseline),
      this.lang,
    )) {
      return;
    }
    this.showStageForm.set(false);
    this.stageError.set(null);
    this.stageFormBaseline = null;
  }

  dismissCreatedFeature(): void {
    this.createdFeature.set(null);
  }

  saveStage(event?: Event): void {
    event?.preventDefault();
    event?.stopPropagation();
    const p = this.project();
    if (!p || this.savingFeature()) return;
    if (!this.stageForm.name.trim()) {
      this.stageError.set('project.errorFeatureName');
      return;
    }
    if (
      this.stageForm.startDate &&
      this.stageForm.endDate &&
      this.stageForm.endDate < this.stageForm.startDate
    ) {
      this.stageError.set('projects.errorDates');
      return;
    }
    this.savingFeature.set(true);
    this.stageError.set(null);
    this.store
      .addStage(this.projectId(), {
        name: this.stageForm.name,
        description: this.stageForm.description,
        startDate: this.stageForm.startDate,
        endDate: this.stageForm.endDate,
        status: 'Planned',
      })
      .subscribe({
        next: (stage) => {
          this.showStageForm.set(false);
          this.stageError.set(null);
          this.stageFormBaseline = null;
          this.savingFeature.set(false);
          this.createdFeature.set({ id: stage.id, name: stage.name });
        },
        error: () => {
          this.savingFeature.set(false);
          this.stageError.set('project.errorFeatureSave');
        },
      });
  }

  addTasksToCreatedFeature(): void {
    const created = this.createdFeature();
    if (!created) return;
    this.createdFeature.set(null);
    this.openTaskForm(created.id);
  }

  openTaskForm(preselectFeatureId?: number | null): void {
    const p = this.project();
    if (!p) return;
    if (
      !allowLeaveIfClean(
        this.showStageForm() && isFormDirty(this.stageForm, this.stageFormBaseline),
        this.lang,
      )
    ) {
      return;
    }
    this.showStageForm.set(false);
    this.stageFormBaseline = null;
    this.createdFeature.set(null);
    this.taskError.set(null);
    const defaultAssignee = this.employees().find((e) => e.name === p.manager);
    const featureId =
      preselectFeatureId ??
      (this.stages().length === 1 ? this.stages()[0].id : null);
    this.taskForm = {
      title: '',
      description: '',
      assigneeId: defaultAssignee?.id ?? null,
      priority: 'MEDIUM',
      estimatedHours: 1,
      startDate: new Date().toISOString().slice(0, 10),
      dueDate: p.endDate || '',
      status: 'TODO',
      stageId: featureId,
    };
    this.taskFormBaseline = formSnapshot(this.taskForm);
    this.setTab('project.tabs.tasks');
    this.showTaskForm.set(true);
  }

  cancelTaskForm(): void {
    if (!allowLeaveIfClean(
      this.showTaskForm() && isFormDirty(this.taskForm, this.taskFormBaseline),
      this.lang,
    )) {
      return;
    }
    this.showTaskForm.set(false);
    this.taskError.set(null);
    this.taskFormBaseline = null;
  }

  saveTask(): void {
    if (!this.taskForm.title.trim()) {
      this.taskError.set('tasks.errorTitle');
      return;
    }
    if (this.stages().length === 0) {
      this.taskError.set('tasks.errorFeatureRequired');
      return;
    }
    if (this.taskForm.stageId == null) {
      this.taskError.set('tasks.errorFeature');
      return;
    }
    if (!(Number(this.taskForm.estimatedHours) > 0)) {
      this.taskError.set('tasks.errorHours');
      return;
    }
    const assignee = this.employees().find((e) => e.id === this.taskForm.assigneeId);
    this.tasksStore.addTask({
      projectId: this.projectId(),
      title: this.taskForm.title,
      description: this.taskForm.description,
      assignee: assignee?.name ?? '',
      assigneeId: this.taskForm.assigneeId,
      priority: this.taskForm.priority,
      estimatedHours: Number(this.taskForm.estimatedHours),
      startDate: this.taskForm.startDate,
      dueDate: this.taskForm.dueDate,
      status: this.taskForm.status,
      stageId: this.taskForm.stageId,
    });
    this.showTaskForm.set(false);
    this.taskError.set(null);
    this.taskFormBaseline = null;
  }

  /** Preview only — backend RiskLevelCalculator is the source of truth. */
  calculatedRiskLevel(): ReturnType<typeof calculateRiskLevel> {
    return calculateRiskLevel(this.riskForm.impact, this.riskForm.probability);
  }

  openRiskForm(): void {
    const p = this.project();
    if (!p) return;
    const profile = this.currentUser.profile();
    const defaultOwner =
      this.employees().find((e) => e.name === p.manager) ??
      this.employees().find((e) => e.id === profile?.employeeId) ??
      this.employees()[0];
    this.riskFormError.set(null);
    this.riskForm = {
      title: '',
      description: '',
      impact: 'MEDIUM',
      probability: 'MEDIUM',
      mitigationPlan: '',
      status: 'OPEN',
      projectId: this.projectId(),
      ownerId: defaultOwner?.id ?? 0,
    };
    this.riskFormBaseline = formSnapshot(this.riskForm);
    this.showRiskForm.set(true);
  }

  cancelRiskForm(): void {
    if (!allowLeaveIfClean(
      this.showRiskForm() && isFormDirty(this.riskForm, this.riskFormBaseline),
      this.lang,
    )) {
      return;
    }
    this.showRiskForm.set(false);
    this.riskFormError.set(null);
    this.riskFormBaseline = null;
  }

  saveRisk(): void {
    if (!this.riskForm.title.trim()) {
      this.riskFormError.set('risks.errorTitle');
      return;
    }
    const owner = this.employees().find((e) => e.id === this.riskForm.ownerId);
    const project = this.project();
    if (!project || !owner) {
      this.riskFormError.set('risks.errorRelations');
      return;
    }
    this.risksStore.addRisk(
      { ...this.riskForm, projectId: this.projectId() },
      project.name,
      owner.name,
    );
    this.showRiskForm.set(false);
    this.riskFormError.set(null);
    this.riskFormBaseline = null;
  }

  openCloseRisk(riskId: number): void {
    this.closingRiskId.set(riskId);
    this.closeRiskError.set(null);
    this.closeRiskForm = { closureReason: '', residualRisk: null };
    this.showCloseRiskModal.set(true);
  }

  cancelCloseRisk(): void {
    this.showCloseRiskModal.set(false);
    this.closingRiskId.set(null);
    this.closeRiskError.set(null);
  }

  confirmCloseRisk(): void {
    const id = this.closingRiskId();
    if (id == null) return;
    if (!this.closeRiskForm.closureReason.trim()) {
      this.closeRiskError.set('risks.closureReasonRequired');
      return;
    }
    this.risksStore.closeRisk(id, {
      closureReason: this.closeRiskForm.closureReason,
      residualRisk: this.closeRiskForm.residualRisk,
    });
    this.cancelCloseRisk();
  }

  completeStage(id: number): void {
    this.store.completeStage(this.projectId(), id);
  }

  deleteStage(id: number): void {
    if (this.openedFeatureId() === id) this.openedFeatureId.set(null);
    this.store.removeStage(this.projectId(), id);
  }

  moveFeature(id: number, direction: 'up' | 'down'): void {
    this.store.moveStage(this.projectId(), id, direction);
  }

  openFeature(id: number): void {
    this.openedFeatureId.update((current) => (current === id ? null : id));
  }

  closeFeature(): void {
    this.openedFeatureId.set(null);
  }

  toggleProgressInfo(event?: Event): void {
    event?.stopPropagation();
    this.showProgressInfo.update((v) => !v);
  }

  formatHours(value: number): string {
    if (!Number.isFinite(value)) return '0';
    return value % 1 === 0 ? String(value) : value.toFixed(1);
  }

  formatDue(iso: string | null | undefined): string {
    if (!iso) return '—';
    const d = new Date(`${iso.slice(0, 10)}T12:00:00`);
    if (Number.isNaN(d.getTime())) return iso;
    return d.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
  }

  stageStatusClass(status: string | null | undefined): string {
    const s = (status ?? '').toUpperCase().replace(/\s+/g, '_');
    if (s === 'DONE') return 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-300';
    if (s === 'ACTIVE') return 'bg-sky-100 text-sky-800 dark:bg-sky-900/40 dark:text-sky-300';
    return 'bg-slate-100 text-slate-700 dark:bg-slate-700 dark:text-slate-200';
  }
}
