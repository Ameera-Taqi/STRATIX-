import { Component, input } from '@angular/core';

@Component({
  selector: 'app-kpi-card',
  standalone: true,
  template: `
    <div
      class="stratix-card border-l-4 p-5"
      [class.border-l-primary]="tone() === 'primary'"
      [class.border-l-success]="tone() === 'success'"
      [class.border-l-warning]="tone() === 'warning'"
      [class.border-l-danger]="tone() === 'danger'"
    >
      <p class="stratix-muted text-sm">{{ label() }}</p>
      <p class="mt-1 text-3xl font-bold text-dark dark:text-slate-100">{{ value() }}</p>
      @if (suffix()) {
        <p class="stratix-muted mt-1 text-xs">{{ suffix() }}</p>
      }
    </div>
  `,
})
export class KpiCardComponent {
  readonly label = input.required<string>();
  readonly value = input.required<string | number>();
  readonly tone = input<'primary' | 'success' | 'warning' | 'danger'>('primary');
  readonly suffix = input<string>();
}
