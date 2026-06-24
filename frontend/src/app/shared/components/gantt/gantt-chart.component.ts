import { Component, computed, input } from '@angular/core';
import { GanttRow, GanttTaskMarker } from './gantt.types';

@Component({
  selector: 'app-gantt-chart',
  standalone: true,
  template: `
    @if (rows().length === 0) {
      <p class="py-8 text-center text-sm stratix-muted">{{ emptyLabel() }}</p>
    } @else {
      <div class="overflow-x-auto">
        <div class="min-w-[640px]">
          <div class="mb-3 flex border-b border-slate-100 pb-2 text-[10px] stratix-muted dark:border-slate-700">
            @for (tick of ticks(); track tick.label) {
              <span class="shrink-0 text-center" [style.width.%]="tick.widthPct">{{ tick.label }}</span>
            }
          </div>
          <div class="space-y-3">
            @for (row of positioned(); track row.id) {
              <div class="grid items-center gap-3" [style.grid-template-columns]="labelWidth() + ' 1fr'">
                <div class="min-w-0">
                  <p class="truncate text-sm font-medium text-dark dark:text-slate-100" [title]="row.label">{{ row.label }}</p>
                  @if (row.sublabel) {
                    <p class="truncate text-[10px] stratix-muted">{{ row.sublabel }}</p>
                  }
                </div>
                <div class="relative h-8 rounded-lg bg-slate-100 dark:bg-slate-700/60">
                  <div
                    class="absolute top-1 h-6 rounded-md shadow-sm"
                    [style.left.%]="row.leftPct"
                    [style.width.%]="row.widthPct"
                    [style.background]="row.color ?? '#3b82f6'"
                    [title]="row.start + ' → ' + row.end"
                  >
                    <div class="h-full overflow-hidden rounded-md bg-black/15">
                      <div class="h-full bg-white/25" [style.width.%]="row.progress"></div>
                    </div>
                  </div>
                  @for (m of markersForRow(row.id); track m.id) {
                    <div
                      class="absolute top-0 h-full w-0.5"
                      [style.left.%]="m.leftPct"
                      [style.background]="m.color ?? '#ef4444'"
                      [title]="m.label + ': ' + m.date"
                    ></div>
                  }
                </div>
              </div>
            }
          </div>
        </div>
      </div>
    }
  `,
})
export class GanttChartComponent {
  readonly rows = input.required<GanttRow[]>();
  readonly markers = input<GanttTaskMarker[]>([]);
  readonly emptyLabel = input('—');
  readonly labelWidth = input('140px');

  readonly range = computed(() => {
    const dates = this.rows().flatMap((r) => [r.start, r.end]);
    const markers = this.markers().map((m) => m.date);
    const all = [...dates, ...markers].map((d) => new Date(d).getTime()).filter((t) => !Number.isNaN(t));
    if (all.length === 0) {
      const now = Date.now();
      return { min: now, max: now + 86400000 };
    }
    const min = Math.min(...all);
    const max = Math.max(...all);
    return { min, max: max === min ? max + 86400000 : max };
  });

  readonly ticks = computed(() => {
    const { min, max } = this.range();
    const span = max - min || 1;
    const count = 6;
    return Array.from({ length: count }, (_, i) => {
      const t = min + (span * i) / (count - 1);
      const d = new Date(t);
      return {
        label: `${d.getMonth() + 1}/${d.getDate()}`,
        widthPct: 100 / count,
      };
    });
  });

  readonly positioned = computed(() => {
    const { min, max } = this.range();
    const span = max - min || 1;
    return this.rows().map((row) => {
      const start = new Date(row.start).getTime();
      const end = new Date(row.end).getTime();
      const leftPct = ((start - min) / span) * 100;
      const widthPct = Math.max(((end - start) / span) * 100, 2);
      return { ...row, leftPct, widthPct: Math.min(widthPct, 100 - leftPct) };
    });
  });

  markersForRow(rowId: number) {
    const { min, max } = this.range();
    const span = max - min || 1;
    return this.markers()
      .filter((m) => m.rowId === rowId)
      .map((m) => ({
        ...m,
        leftPct: ((new Date(m.date).getTime() - min) / span) * 100,
      }));
  }
}
