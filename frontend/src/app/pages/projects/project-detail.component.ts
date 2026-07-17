import { Component, computed, inject, signal } from '@angular/core';

import { ActivatedRoute, RouterLink } from '@angular/router';

import { FormsModule } from '@angular/forms';

import { TopbarComponent } from '../../layout/topbar/topbar.component';

import { ProjectsStore } from '../../core/services/projects.store';

import { TasksStore } from '../../core/services/tasks.store';

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
import { MilestonesStore } from '../../core/services/milestones.store';
import { ChangeRequestsStore } from '../../core/services/change-requests.store';
import { CurrentUserService } from '../../core/services/current-user.service';
import { MilestoneStatus } from '../../core/models/milestone.model';
import { ChangeRequestPriority, ChangeRequestStatus } from '../../core/models/change-request.model';
import { UiIconComponent } from '../../shared/components/ui-icon/ui-icon.component';
import { CHART_COLORS, CHART_SERIES } from '../../shared/components/charts/chart-palette';

const STAGE_COLORS = [...CHART_SERIES];



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

export class ProjectDetailComponent {

  private readonly route = inject(ActivatedRoute);

  private readonly tasksStore = inject(TasksStore);

  private readonly healthService = inject(ProjectHealthService);

  readonly store = inject(ProjectsStore);

  readonly milestonesStore = inject(MilestonesStore);

  readonly changeRequestsStore = inject(ChangeRequestsStore);

  private readonly currentUser = inject(CurrentUserService);



  readonly projectId = Number(this.route.snapshot.paramMap.get('id'));



  readonly project = computed(() => {

    this.store.projects();

    return this.store.getById(this.projectId) ?? this.store.projects()[0];

  });



  readonly stages = computed(() => {

    this.store.stagesByProject();

    return this.store.getStages(this.projectId);

  });



  readonly health = computed(() => {

    this.healthService.all();

    return this.healthService.getByProjectId(this.projectId);

  });



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

      .getByProject(this.projectId)

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

  readonly stageStatusOptions = ['Planned', 'Active', 'Done', 'On Hold'];



  stageForm = {

    name: '',

    startDate: '',

    endDate: '',

    status: 'Planned',

    progress: 0,

  };



  tabKeys = [

    'project.tabs.overview',

    'project.tabs.timeline',

    'project.tabs.stages',

    'project.tabs.milestones',

    'project.tabs.changes',

    'project.tabs.tasks',

    'project.tabs.files',

    'project.tabs.activity',

  ];

  activeTabKey = 'project.tabs.overview';

  readonly milestones = computed(() => {
    this.milestonesStore.milestones();
    return this.milestonesStore.forProject(this.projectId);
  });

  readonly changeRequests = computed(() => {
    this.changeRequestsStore.changeRequests();
    return this.changeRequestsStore.forProject(this.projectId);
  });

  readonly milestoneStatusOptions: MilestoneStatus[] = ['PENDING', 'IN_PROGRESS', 'COMPLETED', 'DELAYED'];
  readonly changeRequestPriorityOptions: ChangeRequestPriority[] = ['LOW', 'MEDIUM', 'HIGH', 'URGENT'];
  readonly changeRequestStatusOptions: ChangeRequestStatus[] = ['PENDING', 'APPROVED', 'REJECTED', 'IMPLEMENTED'];

  readonly showMilestoneForm = signal(false);
  readonly milestoneError = signal<string | null>(null);
  milestoneForm = {
    title: '',
    dueDate: '',
  };

  readonly showChangeRequestForm = signal(false);
  readonly changeRequestError = signal<string | null>(null);
  changeRequestForm = {
    title: '',
    description: '',
    priority: 'MEDIUM' as ChangeRequestPriority,
  };

  setTab(key: string): void {
    this.activeTabKey = key;
    if (key === 'project.tabs.milestones') {
      this.milestonesStore.loadForProject(this.projectId);
    } else if (key === 'project.tabs.changes') {
      this.changeRequestsStore.loadForProject(this.projectId);
    }
  }

  openMilestoneForm(): void {
    this.milestoneError.set(null);
    this.milestoneForm = { title: '', dueDate: this.project().endDate ?? '' };
    this.showMilestoneForm.set(true);
  }

  cancelMilestoneForm(): void {
    this.showMilestoneForm.set(false);
    this.milestoneError.set(null);
  }

  async saveMilestone(): Promise<void> {
    if (!this.milestoneForm.title.trim()) {
      this.milestoneError.set('milestones.errorTitle');
      return;
    }
    if (!this.milestoneForm.dueDate) {
      this.milestoneError.set('milestones.errorDueDate');
      return;
    }
    try {
      await this.milestonesStore.create({
        projectId: this.projectId,
        title: this.milestoneForm.title.trim(),
        dueDate: this.milestoneForm.dueDate,
      });
      this.showMilestoneForm.set(false);
      this.milestoneError.set(null);
    } catch {
      this.milestoneError.set('project.errorStageSave');
    }
  }

  completeMilestone(id: number): void {
    const milestone = this.milestones().find((m) => m.id === id);
    if (!milestone) return;
    void this.milestonesStore.complete(milestone);
  }

  deleteMilestone(id: number): void {
    void this.milestonesStore.remove(id);
  }

  milestoneStatusClass(status: string): string {
    switch (status) {
      case 'COMPLETED':
        return 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300';
      case 'DELAYED':
        return 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300';
      case 'IN_PROGRESS':
        return 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300';
      default:
        return 'bg-slate-100 text-slate-600 dark:bg-slate-700 dark:text-slate-300';
    }
  }

  openChangeRequestForm(): void {
    this.changeRequestError.set(null);
    this.changeRequestForm = { title: '', description: '', priority: 'MEDIUM' };
    this.showChangeRequestForm.set(true);
  }

  cancelChangeRequestForm(): void {
    this.showChangeRequestForm.set(false);
    this.changeRequestError.set(null);
  }

  async saveChangeRequest(): Promise<void> {
    if (!this.changeRequestForm.title.trim()) {
      this.changeRequestError.set('changeRequests.errorTitle');
      return;
    }
    try {
      await this.changeRequestsStore.create({
        projectId: this.projectId,
        title: this.changeRequestForm.title.trim(),
        description: this.changeRequestForm.description.trim() || null,
        priority: this.changeRequestForm.priority,
        requestedById: this.currentUser.profile().employeeId,
      });
      this.showChangeRequestForm.set(false);
      this.changeRequestError.set(null);
    } catch {
      this.changeRequestError.set('changeRequests.errorSave');
    }
  }

  setChangeRequestStatus(id: number, status: ChangeRequestStatus): void {
    const cr = this.changeRequests().find((c) => c.id === id);
    if (!cr) return;
    void this.changeRequestsStore.updateStatus(cr, status, this.currentUser.profile().employeeId);
  }

  deleteChangeRequest(id: number): void {
    void this.changeRequestsStore.remove(id);
  }

  changeRequestStatusClass(status: string): string {
    switch (status) {
      case 'APPROVED':
        return 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300';
      case 'IMPLEMENTED':
        return 'bg-primary/10 text-primary';
      case 'REJECTED':
        return 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300';
      default:
        return 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300';
    }
  }



  openStageForm(): void {

    const p = this.project();

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

    if (!this.stageForm.name.trim()) {

      this.stageError.set('project.errorStageName');

      return;

    }

    if (!this.stageForm.endDate) {

      this.stageError.set('project.errorStageEnd');

      return;

    }

    if (this.stageForm.endDate < this.stageForm.startDate) {

      this.stageError.set('projects.errorDates');

      return;

    }



    this.store.addStage(this.projectId, {
      name: this.stageForm.name,
      startDate: this.stageForm.startDate,
      endDate: this.stageForm.endDate,
      status: this.stageForm.status,
      progress: Number(this.stageForm.progress) || 0,
    }).subscribe({
      next: () => {
        this.showStageForm.set(false);
        this.stageError.set(null);
      },
      error: () => {
        this.stageError.set('project.errorStageSave');
      },
    });
  }



  deleteStage(stageId: number): void {

    this.store.removeStage(this.projectId, stageId);

  }



  completeStage(stageId: number): void {

    this.store.completeStage(this.projectId, stageId);

  }

}

