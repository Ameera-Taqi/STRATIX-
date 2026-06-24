import { Component, input } from '@angular/core';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  template: `<span class="rounded-full px-2.5 py-0.5 text-xs font-medium" [class]="classes()">{{ status() }}</span>`,
})
export class StatusBadgeComponent {
  readonly status = input.required<string>();

  classes(): string {
    const s = this.status().toLowerCase();
    if (['active', 'done', 'completed'].some((x) => s.includes(x)))
      return 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/50 dark:text-emerald-300';
    if (['review', 'planned'].some((x) => s.includes(x)))
      return 'bg-amber-100 text-amber-800 dark:bg-amber-900/50 dark:text-amber-300';
    if (['delayed', 'blocked', 'cancelled'].some((x) => s.includes(x)))
      return 'bg-red-100 text-red-800 dark:bg-red-900/50 dark:text-red-300';
    if (s.includes('progress')) return 'bg-blue-100 text-blue-800 dark:bg-blue-900/50 dark:text-blue-300';
    return 'bg-slate-100 text-slate-700 dark:bg-slate-700 dark:text-slate-300';
  }
}
