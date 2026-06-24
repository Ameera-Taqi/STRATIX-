import { Component, computed, input } from '@angular/core';
import { ChartSegment } from './chart.types';

interface DonutArc {
  label: string;
  value: number;
  color: string;
  dash: number;
  offset: number;
}

@Component({
  selector: 'app-donut-chart',
  standalone: true,
  template: `
    <div class="flex flex-col items-center gap-4 sm:flex-row sm:items-center sm:justify-center">
      <div class="relative shrink-0" [style.width.px]="size()" [style.height.px]="size()">
        <svg [attr.viewBox]="'0 0 ' + view + ' ' + view" class="h-full w-full -rotate-90">
          <circle
            [attr.cx]="center"
            [attr.cy]="center"
            [attr.r]="radius"
            fill="none"
            class="stroke-slate-100 dark:stroke-slate-700"
            [attr.stroke-width]="stroke"
          />
          @for (arc of arcs(); track arc.label) {
            <circle
              [attr.cx]="center"
              [attr.cy]="center"
              [attr.r]="radius"
              fill="none"
              [attr.stroke]="arc.color"
              [attr.stroke-width]="stroke"
              stroke-linecap="round"
              [attr.stroke-dasharray]="arc.dash + ' ' + circumference"
              [attr.stroke-dashoffset]="-arc.offset"
            />
          }
        </svg>
        @if (centerLabel()) {
          <div class="pointer-events-none absolute inset-0 flex flex-col items-center justify-center text-center">
            <span class="text-2xl font-bold text-dark dark:text-slate-100">{{ centerLabel() }}</span>
            @if (centerSubLabel()) {
              <span class="text-[10px] stratix-muted">{{ centerSubLabel() }}</span>
            }
          </div>
        }
      </div>
      @if (showLegend()) {
        <ul class="grid gap-2 text-xs">
          @for (seg of segments(); track seg.label) {
            <li class="flex items-center gap-2">
              <span class="h-2.5 w-2.5 shrink-0 rounded-full" [style.background]="seg.color"></span>
              <span class="stratix-muted">{{ seg.label }}</span>
              <span class="ms-auto font-semibold text-dark dark:text-slate-100">{{ seg.value }}</span>
            </li>
          }
        </ul>
      }
    </div>
  `,
})
export class DonutChartComponent {
  readonly segments = input.required<ChartSegment[]>();
  readonly size = input(160);
  readonly centerLabel = input<string>();
  readonly centerSubLabel = input<string>();
  readonly showLegend = input(true);

  protected readonly view = 100;
  protected readonly center = 50;
  protected readonly radius = 38;
  protected readonly stroke = 14;
  protected readonly circumference = 2 * Math.PI * this.radius;

  readonly arcs = computed(() => {
    const total = this.segments().reduce((sum, s) => sum + s.value, 0) || 1;
    let offset = 0;
    return this.segments().map((seg): DonutArc => {
      const dash = (seg.value / total) * this.circumference;
      const arc = { label: seg.label, value: seg.value, color: seg.color, dash, offset };
      offset += dash;
      return arc;
    });
  });
}
