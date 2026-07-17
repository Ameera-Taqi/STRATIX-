import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { ProjectsStore } from '../../core/services/projects.store';
import { TasksStore } from '../../core/services/tasks.store';
import { GanttChartComponent } from '../../shared/components/gantt/gantt-chart.component';
import { GanttRow, GanttTaskMarker } from '../../shared/components/gantt/gantt.types';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { UiIconComponent } from '../../shared/components/ui-icon/ui-icon.component';
import { CHART_COLORS, CHART_SERIES } from '../../shared/components/charts/chart-palette';

const STAGE_COLORS = [...CHART_SERIES];

@Component({
  selector: 'app-timeline',
  standalone: true,
  imports: [TopbarComponent, RouterLink, GanttChartComponent, TranslatePipe, UiIconComponent],
  template: `
    <app-topbar titleKey="page.timeline" />
    <main class="stratix-page p-6">
      <p class="mb-6 text-sm stratix-muted">{{ 'timeline.hint' | t }}</p>

      @for (block of projectBlocks(); track block.projectId) {
        <section class="stratix-card mb-6 overflow-hidden">
          <div class="stratix-card-header flex items-center justify-between">
            <div>
              <h3 class="stratix-heading text-sm">
                <a [routerLink]="['/projects', block.projectId]" class="text-primary hover:underline">{{ block.projectName }}</a>
              </h3>
              <p class="mt-1 text-xs stratix-muted">
                {{ block.start }} → {{ block.end }} · {{ block.progress }}%
              </p>
            </div>
            <a [routerLink]="['/projects', block.projectId]" class="inline-flex items-center gap-1 text-xs text-primary hover:underline">{{ 'common.view' | t }} <span class="stratix-chevron-flip"><app-ui-icon name="chevron-right" size="xs" /></span></a>
          </div>
          <div class="p-5">
            <p class="mb-3 text-xs font-medium stratix-muted">{{ 'timeline.stages' | t }}</p>
            <app-gantt-chart [rows]="block.stageRows" [markers]="block.taskMarkers" [emptyLabel]="'timeline.noStages' | t" />
          </div>
        </section>
      }
    </main>
  `,
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
export class TimelineComponent {
  private readonly projects = inject(ProjectsStore);
  private readonly tasks = inject(TasksStore);

  readonly projectBlocks = computed(() => {
    this.projects.projects();
    this.projects.stagesByProject();
    this.tasks.tasks();

    return this.projects.projects().map((p) => {
      const stages = this.projects.getStages(p.id);
      const stageRows: GanttRow[] = stages.map((s, i) => ({
        id: s.id,
        label: s.name,
        start: s.startDate,
        end: s.endDate,
        progress: s.progress,
        color: STAGE_COLORS[i % STAGE_COLORS.length],
        sublabel: s.status,
      }));

      const projectTasks = this.tasks.getByProject(p.id);
      const taskMarkers: GanttTaskMarker[] = projectTasks
        .filter((t) => t.stageId != null)
        .map((t, i) => ({
          id: i + 1,
          rowId: t.stageId!,
          label: t.title,
          date: t.dueDate,
          color: t.status === 'DONE' ? CHART_COLORS.teal : CHART_COLORS.coral,
        }));

      return {
        projectId: p.id,
        projectName: p.name,
        start: p.startDate,
        end: p.endDate,
        progress: p.progress,
        stageRows,
        taskMarkers,
      };
    });
  });
}
