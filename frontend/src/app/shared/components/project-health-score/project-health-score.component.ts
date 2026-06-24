import { Component, input } from '@angular/core';
import { ProjectHealthResult } from '../../utils/project-health.util';
import { healthStatusClass } from '../../utils/project-health.util';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

@Component({
  selector: 'app-project-health-score',
  standalone: true,
  imports: [TranslatePipe],
  template: `
    @if (health()) {
      @if (compact()) {
        <div class="flex items-center gap-2">
          <span
            class="inline-flex h-9 w-9 items-center justify-center rounded-full text-xs font-bold"
            [class]="healthStatusClass(health()!.status)"
          >
            {{ health()!.score }}
          </span>
          <span class="stratix-badge" [class]="healthStatusClass(health()!.status)">
            {{ 'dashboard.health.' + health()!.status | t }}
          </span>
        </div>
      } @else {
        <div class="flex flex-wrap items-center gap-4">
          <div
            class="flex h-20 w-20 flex-col items-center justify-center rounded-full border-4 text-center"
            [class]="healthStatusClass(health()!.status)"
          >
            <span class="text-2xl font-bold">{{ health()!.score }}</span>
            <span class="text-[9px] uppercase tracking-wide opacity-80">{{ 'health.score' | t }}</span>
          </div>
          <div class="min-w-0 flex-1">
            <p class="text-sm font-semibold text-dark dark:text-slate-100">{{ 'health.projectHealthScore' | t }}</p>
            <span class="stratix-badge mt-1" [class]="healthStatusClass(health()!.status)">
              {{ 'dashboard.health.' + health()!.status | t }}
            </span>
            <p class="mt-2 text-xs stratix-muted">{{ health()!.noteKey | t }}</p>
            <div class="mt-3 h-2 max-w-xs overflow-hidden rounded-full bg-slate-100 dark:bg-slate-700">
              <div class="h-full rounded-full bg-primary" [style.width.%]="health()!.score"></div>
            </div>
          </div>
        </div>
      }
    }
  `,
})
export class ProjectHealthScoreComponent {
  readonly health = input<ProjectHealthResult | null | undefined>();
  readonly compact = input(false);
  readonly healthStatusClass = healthStatusClass;
}
