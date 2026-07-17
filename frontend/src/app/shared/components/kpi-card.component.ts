import { Component, input } from '@angular/core';

@Component({
  selector: 'app-kpi-card',
  standalone: true,
  host: { class: 'block h-full' },
  template: `
    <div
      class="stratix-card flex h-full min-h-[7.5rem] flex-col border-s-4 p-5"
      [class.border-s-primary]="tone() === 'primary'"
      [class.border-s-success]="tone() === 'success'"
      [class.border-s-warning]="tone() === 'warning'"
      [class.border-s-danger]="tone() === 'danger'"
    >
      <p class="stratix-muted text-sm">{{ label() }}</p>
      <p class="mt-1 text-3xl font-bold text-dark dark:text-slate-100">{{ value() }}</p>
      <p class="stratix-muted mt-auto pt-1 text-xs leading-4">
        {{ suffix() || '\u00A0' }}
      </p>
    </div>
  `,
})
export class KpiCardComponent {
  readonly label = input.required<string>();
  readonly value = input.required<string | number>();
  readonly tone = input<'primary' | 'success' | 'warning' | 'danger'>('primary');
  readonly suffix = input<string>();
}
