import { Component, computed, input } from '@angular/core';
import { GroupedBarItem, GroupedBarSeries } from './chart.types';

@Component({
  selector: 'app-grouped-bar-chart',
  standalone: true,
  template: `
    <div>
      <div class="mb-4 flex flex-wrap gap-3 text-xs">
        @for (s of series(); track s.key) {
          <span class="flex items-center gap-1.5">
            <span class="h-2.5 w-2.5 rounded-sm" [style.background]="s.color"></span>
            <span class="stratix-muted">{{ s.label }}</span>
          </span>
        }
      </div>
      @if (normalized().length === 0) {
        <p class="py-8 text-center text-xs stratix-muted">{{ emptyLabel() }}</p>
      } @else {
        <div class="flex items-end justify-around gap-3" [style.height.px]="height()">
          @for (group of normalized(); track group.label) {
            <div class="flex min-w-0 flex-1 flex-col items-center gap-1">
              <div
                class="flex w-full max-w-[72px] items-end justify-center gap-1"
                [style.height.px]="plotHeight()"
              >
                @for (bar of group.bars; track bar.key) {
                  <div
                    class="w-full rounded-t-[10px] transition-all dark:shadow-[0_0_14px_color-mix(in_srgb,var(--bar)_40%,transparent)]"
                    [style.--bar]="bar.color"
                    [style.height.px]="bar.barHeight"
                    [style.background]="'linear-gradient(180deg, color-mix(in srgb, ' + bar.color + ' 90%, white) 0%, ' + bar.color + ' 40%, color-mix(in srgb, ' + bar.color + ' 85%, black) 100%)'"
                    [title]="bar.label + ': ' + bar.value"
                  ></div>
                }
              </div>
              <span class="w-full truncate text-center text-[10px] stratix-muted" [title]="group.label">
                {{ group.label }}
              </span>
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class GroupedBarChartComponent {
  readonly items = input.required<GroupedBarItem[]>();
  readonly series = input.required<GroupedBarSeries[]>();
  readonly height = input(200);
  readonly emptyLabel = input('—');

  readonly plotHeight = computed(() => Math.max(this.height() - 28, 60));

  readonly normalized = computed(() => {
    const series = this.series();
    const items = this.items();
    if (items.length === 0) return [];

    const max = Math.max(
      ...items.flatMap((item) => series.map((s) => item.values[s.key] ?? 0)),
      1,
    );
    const plot = this.plotHeight();

    return items.map((item) => ({
      label: item.label,
      bars: series.map((s) => {
        const value = item.values[s.key] ?? 0;
        return {
          key: s.key,
          label: s.label,
          color: s.color,
          value,
          barHeight: Math.max((value / max) * plot, 3),
        };
      }),
    }));
  });
}
