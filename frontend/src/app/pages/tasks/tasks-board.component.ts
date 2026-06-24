import { Component, computed, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { TaskBoardComponent } from '../../shared/components/task-board/task-board.component';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { ProjectsStore } from '../../core/services/projects.store';

@Component({
  selector: 'app-tasks-board',
  standalone: true,
  imports: [TopbarComponent, TaskBoardComponent, TranslatePipe, RouterLink],
  template: `
    <app-topbar titleKey="page.tasks" />
    <main class="stratix-page p-6">
      @if (filterProjectId()) {
        <p class="stratix-muted mb-4 text-sm">
          {{ 'tasks.filtered' | t }}:
          <strong>{{ projectName() }}</strong>
          <a routerLink="/tasks" class="ms-2 text-primary hover:underline">{{ 'tasks.showAll' | t }}</a>
        </p>
      }
      <app-task-board [projectId]="filterProjectId()" />
    </main>
  `,
})
export class TasksBoardComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly projectsStore = inject(ProjectsStore);

  readonly filterProjectId = toSignal(
    this.route.queryParamMap.pipe(map((p) => {
      const v = p.get('project');
      return v ? Number(v) : null;
    })),
    { initialValue: null as number | null }
  );

  readonly projectName = computed(() => {
    const id = this.filterProjectId();
    if (id == null) return '';
    return this.projectsStore.getById(id)?.name ?? '';
  });
}
