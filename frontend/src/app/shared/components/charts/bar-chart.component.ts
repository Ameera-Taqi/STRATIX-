import { Component, computed, input } from '@angular/core';
import { BarItem } from './chart.types';

@Component({
  selector: 'app-bar-chart',
  standalone: true,
  template: `
    <div class="flex h-full flex-col">
      @if (normalized().length === 0) {
        <p class="py-8 text-center text-xs stratix-muted">{{ emptyLabel() }}</p>
      } @else {
        <div class="flex items-end justify-around gap-2 px-1" [style.height.px]="height()">
          @for (bar of normalized(); track bar.label) {
            <div class="flex min-w-0 flex-1 flex-col items-center gap-1">
              <span class="text-[10px] font-semibold text-dark dark:text-slate-200">{{ bar.value }}</span>
              <div
                class="flex w-full max-w-[48px] items-end justify-center"
                [style.height.px]="plotHeight()"
              >
                <div
                  class="w-full rounded-t-md transition-all"
                  [style.height.px]="bar.barHeight"
                  [style.background]="bar.color ?? '#3b82f6'"
                  [title]="bar.label + ': ' + bar.value"
                ></div>
              </div>
              <span class="w-full truncate text-center text-[10px] stratix-muted" [title]="bar.label">
                {{ bar.label }}
              </span>
              @if (bar.sublabel) {
                <span class="w-full truncate text-center text-[9px] text-slate-400">{{ bar.sublabel }}</span>
              }
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class BarChartComponent {
  readonly items = input.required<BarItem[]>();
  readonly height = input(180);
  readonly maxValue = input<number>();
  readonly emptyLabel = input('—');

  readonly plotHeight = computed(() => Math.max(this.height() - 48, 40));

  readonly normalized = computed(() => {
    const items = this.items();
    if (items.length === 0) return [];

    const max = this.maxValue() ?? Math.max(...items.map((i) => i.value), 1);
    const plot = this.plotHeight();

    return items.map((item) => ({
      ...item,
      barHeight: Math.max((item.value / max) * plot, 4),
    }));
  });
}
