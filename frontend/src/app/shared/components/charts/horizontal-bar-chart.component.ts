import { Component, computed, input } from '@angular/core';
import { BarItem } from './chart.types';

@Component({
  selector: 'app-horizontal-bar-chart',
  standalone: true,
  template: `
    <div class="space-y-3">
      @for (bar of normalized(); track bar.label) {
        <div>
          <div class="mb-1 flex items-center justify-between gap-2 text-xs">
            <span class="truncate font-medium text-dark dark:text-slate-100" [title]="bar.label">{{ bar.label }}</span>
            <span class="shrink-0 font-semibold text-dark dark:text-slate-200">{{ bar.value }}%</span>
          </div>
          <div class="h-3 overflow-hidden rounded-full bg-slate-100 dark:bg-slate-700">
            <div
              class="h-full rounded-full transition-all"
              [style.width.%]="bar.pct"
              [style.background]="bar.color ?? '#3b82f6'"
            ></div>
          </div>
          @if (bar.sublabel) {
            <p class="mt-0.5 text-[10px] stratix-muted">{{ bar.sublabel }}</p>
          }
        </div>
      }
    </div>
  `,
})
export class HorizontalBarChartComponent {
  readonly items = input.required<BarItem[]>();
  readonly maxValue = input(100);

  readonly normalized = computed(() => {
    const max = this.maxValue();
    return this.items().map((item) => ({
      ...item,
      pct: Math.min(Math.max((item.value / max) * 100, 2), 100),
    }));
  });
}
