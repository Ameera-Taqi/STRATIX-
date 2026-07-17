import { Component, computed, input } from '@angular/core';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { projectStatusLabelKey, taskStatusLabelKey } from '../utils/enum-labels';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [TranslatePipe],
  template: `<span class="rounded-full px-2.5 py-0.5 text-xs font-medium" [class]="classes()">{{ labelKey() | t }}</span>`,
})
export class StatusBadgeComponent {
  readonly status = input.required<string>();

  readonly labelKey = computed(() => {
    const raw = this.status();
    const upper = raw.trim().toUpperCase().replace(/[\s-]+/g, '_');
    if (['TODO', 'IN_PROGRESS', 'INPROGRESS'].includes(upper)) {
      return taskStatusLabelKey(raw);
    }
    return projectStatusLabelKey(raw);
  });

  classes(): string {
    const s = this.status().toLowerCase();
    if (['active', 'done', 'completed', 'closed'].some((x) => s.includes(x)))
      return 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/50 dark:text-emerald-300';
    if (['review', 'planned', 'mitigating'].some((x) => s.includes(x)))
      return 'bg-amber-100 text-amber-800 dark:bg-amber-900/50 dark:text-amber-300';
    if (['delayed', 'blocked', 'cancelled', 'canceled', 'open'].some((x) => s.includes(x)))
      return 'bg-red-100 text-red-800 dark:bg-red-900/50 dark:text-red-300';
    if (s.includes('progress') || s.includes('hold'))
      return 'bg-blue-100 text-blue-800 dark:bg-blue-900/50 dark:text-blue-300';
    return 'bg-slate-100 text-slate-700 dark:bg-slate-700 dark:text-slate-300';
  }
}
