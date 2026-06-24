import { Component, computed, input } from '@angular/core';
import { ProjectHealthFactors } from '../../utils/project-health.util';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

@Component({
  selector: 'app-health-breakdown',
  standalone: true,
  imports: [TranslatePipe],
  template: `
    @if (factors()) {
      <p class="mb-3 rounded-lg bg-slate-50 px-3 py-2 font-mono text-xs text-slate-600 dark:bg-slate-800/60 dark:text-slate-300">
        {{ 'health.formula' | t }}
      </p>
      <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        @for (item of items(); track item.key) {
          <div class="rounded-lg border border-slate-100 bg-slate-50/80 p-3 dark:border-slate-700 dark:bg-slate-800/50">
            <p class="text-xs stratix-muted">{{ item.labelKey | t }}</p>
            <p class="mt-1 text-2xl font-bold" [class]="item.tone">{{ item.display }}</p>
          </div>
        }
      </div>
    }
  `,
})
export class HealthBreakdownComponent {
  readonly factors = input<ProjectHealthFactors | null | undefined>();

  readonly items = computed(() => {
    const f = this.factors();
    if (!f) return [];
    return [
      { key: 'progress', labelKey: 'health.factorProgress', display: `+${f.progress}`, tone: 'text-emerald-600 dark:text-emerald-300' },
      { key: 'onTime', labelKey: 'health.factorOnTime', display: `+${f.onTimeTasks}`, tone: 'text-emerald-600 dark:text-emerald-300' },
      { key: 'delayed', labelKey: 'health.factorDelayed', display: `−${f.delayedTasks}`, tone: 'text-red-600 dark:text-red-300' },
      { key: 'critical', labelKey: 'health.factorCriticalRisks', display: `−${f.criticalRisks}`, tone: 'text-red-600 dark:text-red-300' },
    ];
  });
}
