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



const STAGE_COLORS = ['#3b82f6', '#10b981', '#f59e0b', '#8b5cf6', '#ef4444'];



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

  ],

  templateUrl: './project-detail.component.html',

})

export class ProjectDetailComponent {

  private readonly route = inject(ActivatedRoute);

  private readonly tasksStore = inject(TasksStore);

  private readonly healthService = inject(ProjectHealthService);

  readonly store = inject(ProjectsStore);



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

        color: t.status === 'DONE' ? '#10b981' : '#ef4444',

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

    'project.tabs.tasks',

    'project.tabs.files',

    'project.tabs.activity',

  ];

  activeTabKey = 'project.tabs.overview';



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

