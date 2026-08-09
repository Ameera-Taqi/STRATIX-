import { Component, computed, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { CHART_SERIES } from '../charts/chart-palette';
import { UiIconComponent } from '../ui-icon/ui-icon.component';

export interface TimelineFeature {
  id: number;
  name: string;
  startDate: string;
  endDate: string;
  progress: number;
  status: string;
}

export interface TimelineTask {
  id: number;
  title: string;
  stageId: number | null;
  startDate?: string;
  dueDate: string;
  status: string;
}

interface TimelineBar {
  key: string;
  kind: 'feature' | 'task';
  id: number;
  label: string;
  status: string;
  start: string;
  end: string;
  progress: number;
  color: string;
  depth: 0 | 1;
  link: string[] | null;
  leftPct: number;
  widthPct: number;
  parentId?: number;
}

const DAY_MS = 86400000;
const MONTHS_EN = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
const MONTHS_AR = ['ينا', 'فبر', 'مار', 'أبر', 'ماي', 'يون', 'يول', 'أغس', 'سبت', 'أكت', 'نوف', 'ديس'];

@Component({
  selector: 'app-project-timeline',
  standalone: true,
  imports: [TranslatePipe, RouterLink, UiIconComponent],
  template: `
    @if (bars().length === 0) {
      <div class="px-6 py-14 text-center">
        <p class="text-sm stratix-muted">{{ emptyLabel() }}</p>
      </div>
    } @else {
      <div class="overflow-x-auto">
        <div class="min-w-[880px]">
          <!-- Calendar header -->
          <div
            class="sticky top-0 z-20 grid border-b border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-950"
            [style.grid-template-columns]="labelCol() + ' 1fr'"
          >
            <div class="border-e border-slate-200 px-4 py-3 text-[11px] font-semibold uppercase tracking-wide stratix-muted dark:border-slate-700">
              {{ 'timeline.workItems' | t }}
            </div>
            <div class="relative min-h-[52px]">
              <div class="absolute inset-0 flex">
                @for (m of months(); track m.key) {
                  <div
                    class="flex flex-col justify-end border-e border-slate-100 px-1 pb-2 dark:border-slate-800"
                    [style.width.%]="m.widthPct"
                  >
                    <span class="truncate text-center text-[11px] font-semibold text-slate-600 dark:text-slate-300">
                      {{ m.label }}
                    </span>
                  </div>
                }
              </div>
              @if (todayLeft() != null) {
                <div
                  class="pointer-events-none absolute bottom-0 top-0 z-10 w-px bg-rose-500"
                  [style.left.%]="todayLeft()!"
                ></div>
              }
            </div>
          </div>

          <!-- Rows -->
          <div class="divide-y divide-slate-100 dark:divide-slate-800">
            @for (bar of visibleBars(); track bar.key) {
              <div
                class="group grid min-h-[48px] items-stretch hover:bg-slate-50/80 dark:hover:bg-slate-900/40"
                [style.grid-template-columns]="labelCol() + ' 1fr'"
              >
                <div
                  class="flex items-center gap-2 border-e border-slate-200 px-3 py-2 dark:border-slate-700"
                  [class.ps-8]="bar.depth === 1"
                >
                  @if (bar.kind === 'feature') {
                    <button
                      type="button"
                      class="inline-flex h-5 w-5 shrink-0 items-center justify-center rounded text-slate-500 hover:bg-slate-200 dark:hover:bg-slate-700"
                      [attr.aria-expanded]="!isCollapsed(bar.id)"
                      [attr.aria-label]="'timeline.toggleFeature' | t"
                      (click)="toggleFeature(bar.id)"
                    >
                      <span
                        class="inline-flex transition"
                        [class.rotate-90]="!isCollapsed(bar.id)"
                      >
                        <app-ui-icon name="chevron-right" size="xs" />
                      </span>
                    </button>
                    <span class="h-2 w-2 shrink-0 rounded-sm" [style.background]="bar.color"></span>
                    <div class="min-w-0 flex-1">
                      <p class="truncate text-sm font-semibold text-dark dark:text-slate-100" [title]="bar.label">
                        {{ bar.label }}
                      </p>
                      <p class="truncate text-[10px] stratix-muted">{{ bar.status }} · {{ bar.progress }}%</p>
                    </div>
                  } @else {
                    <span class="ms-1 h-1.5 w-1.5 shrink-0 rounded-full bg-sky-500"></span>
                    <div class="min-w-0 flex-1">
                      @if (bar.link) {
                        <a
                          [routerLink]="bar.link"
                          class="block truncate text-sm text-primary hover:underline"
                          [title]="bar.label"
                        >
                          {{ bar.label }}
                        </a>
                      } @else {
                        <p class="truncate text-sm text-dark dark:text-slate-100" [title]="bar.label">{{ bar.label }}</p>
                      }
                      <p class="truncate text-[10px] stratix-muted">{{ bar.status }}</p>
                    </div>
                  }
                </div>

                <div class="relative">
                  <div class="pointer-events-none absolute inset-0 flex">
                    @for (m of months(); track m.key) {
                      <div class="border-e border-slate-100 dark:border-slate-800" [style.width.%]="m.widthPct"></div>
                    }
                  </div>

                  @if (todayLeft() != null) {
                    <div
                      class="pointer-events-none absolute bottom-0 top-0 z-[1] w-px bg-rose-500/80"
                      [style.left.%]="todayLeft()!"
                    ></div>
                  }

                  @if (bar.widthPct > 0) {
                    <div
                      class="absolute top-1/2 z-[2] -translate-y-1/2 overflow-hidden rounded-md shadow-sm"
                      [class.h-7]="bar.kind === 'feature'"
                      [class.h-5]="bar.kind === 'task'"
                      [style.left.%]="bar.leftPct"
                      [style.width.%]="bar.widthPct"
                      [style.background]="bar.kind === 'feature' ? bar.color : 'color-mix(in srgb, ' + bar.color + ' 55%, white)'"
                      [title]="bar.start + ' → ' + bar.end"
                    >
                      <div class="h-full bg-black/10" [style.width.%]="bar.progress"></div>
                      <span
                        class="pointer-events-none absolute inset-x-2 top-1/2 -translate-y-1/2 truncate text-[10px] font-semibold text-white drop-shadow"
                        [class.opacity-0]="bar.widthPct < 8"
                      >
                        {{ bar.label }}
                      </span>
                    </div>
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
export class ProjectTimelineComponent {
  readonly features = input.required<TimelineFeature[]>();
  readonly tasks = input<TimelineTask[]>([]);
  readonly emptyLabel = input('—');
  readonly labelCol = input('240px');
  readonly locale = input<'en' | 'ar'>('en');

  readonly collapsed = signal<Record<number, boolean>>({});

  readonly range = computed(() => {
    const dates: number[] = [];
    for (const f of this.features()) {
      const s = this.parse(f.startDate);
      const e = this.parse(f.endDate) ?? s;
      if (s != null) dates.push(s);
      if (e != null) dates.push(e);
    }
    for (const t of this.tasks()) {
      const s = this.parse(t.startDate || t.dueDate);
      const e = this.parse(t.dueDate) ?? s;
      if (s != null) dates.push(s);
      if (e != null) dates.push(e);
    }
    if (dates.length === 0) {
      const now = Date.now();
      return { min: now - 14 * DAY_MS, max: now + 60 * DAY_MS };
    }
    let min = Math.min(...dates);
    let max = Math.max(...dates);
    // Pad so bars aren't flush to edges (Jira-like breathing room).
    min -= 7 * DAY_MS;
    max += 14 * DAY_MS;
    if (max <= min) max = min + 30 * DAY_MS;
    return { min, max };
  });

  readonly months = computed(() => {
    const { min, max } = this.range();
    const span = max - min || 1;
    const months: { key: string; label: string; widthPct: number }[] = [];
    const start = new Date(min);
    start.setDate(1);
    start.setHours(0, 0, 0, 0);
    let cursor = start.getTime();
    const monthNames = this.locale() === 'ar' ? MONTHS_AR : MONTHS_EN;
    let guard = 0;
    while (cursor < max && guard < 36) {
      const d = new Date(cursor);
      const next = new Date(d.getFullYear(), d.getMonth() + 1, 1).getTime();
      const left = Math.max(cursor, min);
      const right = Math.min(next, max);
      const widthPct = ((right - left) / span) * 100;
      if (widthPct > 0.2) {
        months.push({
          key: `${d.getFullYear()}-${d.getMonth()}`,
          label: `${monthNames[d.getMonth()]} ${d.getFullYear()}`,
          widthPct,
        });
      }
      cursor = next;
      guard += 1;
    }
    return months;
  });

  readonly todayLeft = computed(() => {
    const { min, max } = this.range();
    const now = Date.now();
    if (now < min || now > max) return null;
    return ((now - min) / (max - min || 1)) * 100;
  });

  readonly bars = computed((): TimelineBar[] => {
    const { min, max } = this.range();
    const span = max - min || 1;
    const tasks = this.tasks();
    const rows: TimelineBar[] = [];

    this.features().forEach((f, i) => {
      const color = CHART_SERIES[i % CHART_SERIES.length];
      const placed = this.place(f.startDate, f.endDate || f.startDate, min, span);
      rows.push({
        key: `f-${f.id}`,
        kind: 'feature',
        id: f.id,
        label: f.name,
        status: f.status,
        start: f.startDate,
        end: f.endDate || f.startDate,
        progress: Number(f.progress) || 0,
        color,
        depth: 0,
        link: null,
        ...placed,
      });

      const children = tasks.filter((t) => Number(t.stageId) === Number(f.id));
      for (const t of children) {
        const start = t.startDate || t.dueDate;
        const end = t.dueDate || start;
        const childPlaced = this.place(start, end, min, span);
        rows.push({
          key: `t-${t.id}`,
          kind: 'task',
          id: t.id,
          label: t.title,
          status: t.status,
          start,
          end,
          progress: t.status === 'DONE' ? 100 : t.status === 'IN_PROGRESS' || t.status === 'REVIEW' ? 50 : 0,
          color,
          depth: 1,
          link: ['/tasks', String(t.id)],
          parentId: f.id,
          ...childPlaced,
        });
      }
    });

    // Orphan tasks (no feature)
    for (const t of tasks.filter((x) => x.stageId == null)) {
      const start = t.startDate || t.dueDate;
      const end = t.dueDate || start;
      const placed = this.place(start, end, min, span);
      rows.push({
        key: `t-${t.id}`,
        kind: 'task',
        id: t.id,
        label: t.title,
        status: t.status,
        start,
        end,
        progress: t.status === 'DONE' ? 100 : 0,
        color: CHART_SERIES[0],
        depth: 0,
        link: ['/tasks', String(t.id)],
        ...placed,
      });
    }

    return rows;
  });

  readonly visibleBars = computed(() => {
    const collapsed = this.collapsed();
    return this.bars().filter((b) => {
      if (b.kind === 'task' && b.parentId != null && collapsed[b.parentId]) return false;
      return true;
    });
  });

  isCollapsed(featureId: number): boolean {
    return !!this.collapsed()[featureId];
  }

  toggleFeature(featureId: number): void {
    this.collapsed.update((map) => ({ ...map, [featureId]: !map[featureId] }));
  }

  private place(startRaw: string, endRaw: string, min: number, span: number) {
    let start = this.parse(startRaw);
    let end = this.parse(endRaw);
    if (start == null && end == null) return { leftPct: 0, widthPct: 0 };
    if (start == null) start = end!;
    if (end == null || end < start) end = start + DAY_MS;
    const leftPct = Math.max(0, Math.min(100, ((start - min) / span) * 100));
    const rawWidth = Math.max(((end - start) / span) * 100, 1.5);
    const widthPct = Math.max(0, Math.min(rawWidth, 100 - leftPct));
    return { leftPct, widthPct };
  }

  private parse(value: string | null | undefined): number | null {
    if (!value) return null;
    const t = new Date(`${String(value).slice(0, 10)}T12:00:00`).getTime();
    return Number.isFinite(t) ? t : null;
  }
}
