import { Component, computed, input } from '@angular/core';
import { BarItem } from './chart.types';
import { chartBarFill, chartSeriesColor } from './chart-palette';

/** Room reserved above each bar for its value label. */
const VALUE_LABEL_HEIGHT = 18;

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
            <div class="group flex min-w-0 flex-1 flex-col items-center gap-1">
              <div
                class="flex w-full max-w-[48px] flex-col items-center justify-end"
                [style.height.px]="plotHeight()"
              >
                <span class="mb-1 text-[10px] font-semibold text-dark dark:text-slate-200">{{ bar.value }}</span>
                <div
                  class="w-[72%] max-w-[40px] rounded-t-[10px] transition-all group-hover:brightness-110 dark:shadow-[0_0_16px_color-mix(in_srgb,var(--bar)_45%,transparent)]"
                  [style.--bar]="bar.fillColor"
                  [style.height.px]="bar.barHeight"
                  [style.background]="bar.fill"
                  [title]="bar.label + ': ' + bar.value"
                ></div>
              </div>
              <span class="w-full truncate text-center text-[10px] stratix-muted" [title]="bar.label">
                {{ bar.label }}
              </span>
              @if (bar.sublabel) {
                <span class="w-full truncate text-center text-[9px] text-slate-400 dark:text-slate-500">{{ bar.sublabel }}</span>
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

  /** The value label sits directly above each bar, so bars scale into the space left under it. */
  private readonly barTrack = computed(() => Math.max(this.plotHeight() - VALUE_LABEL_HEIGHT, 24));

  readonly normalized = computed(() => {
    const items = this.items();
    if (items.length === 0) return [];

    const max = this.maxValue() ?? Math.max(...items.map((i) => i.value), 1);
    const track = this.barTrack();

    return items.map((item, index) => {
      const fillColor = item.color ?? chartSeriesColor(index);
      return {
        ...item,
        fillColor,
        fill: chartBarFill(fillColor),
        barHeight: Math.max((item.value / max) * track, 4),
      };
    });
  });
}
