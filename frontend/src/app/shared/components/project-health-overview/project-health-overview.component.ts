import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { ProjectHealthResult, healthStatusClass } from '../../utils/project-health.util';
import {
  trendPointsFromSnapshots,
  weekDeltaFromSnapshots,
} from '../../utils/health-trend.util';
import { ProjectHealthSnapshot } from '../../../core/models/health-snapshot.model';
import { ApiService } from '../../../core/services/api.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

@Component({
  selector: 'app-project-health-overview',
  standalone: true,
  imports: [TranslatePipe],
  template: `
    <section class="stratix-card overflow-hidden border border-emerald-200/70 dark:border-emerald-900/40">
      <div class="bg-gradient-to-br from-emerald-50 via-white to-sky-50 px-6 py-6 dark:from-emerald-950/40 dark:via-slate-950 dark:to-sky-950/30">
        <div class="flex flex-wrap items-start justify-between gap-4">
          <div>
            <p class="text-[11px] font-bold uppercase tracking-[0.16em] text-emerald-700 dark:text-emerald-300">
              {{ 'health.projectHealth' | t }}
            </p>
            @if (health(); as h) {
              <div class="mt-2 flex flex-wrap items-end gap-3">
                <span class="text-5xl font-bold tabular-nums tracking-tight text-dark dark:text-slate-100">
                  {{ h.score }}
                </span>
                <span
                  class="mb-1 inline-flex items-center rounded-full px-3 py-1 text-xs font-bold uppercase tracking-wide"
                  [class]="healthStatusClass(h.status)"
                >
                  {{ 'dashboard.health.' + h.status | t }}
                </span>
              </div>
              @if (weekDelta(); as delta) {
                <p
                  class="mt-2 text-sm font-medium"
                  [class.text-emerald-700]="delta.direction === 'up'"
                  [class.dark:text-emerald-300]="delta.direction === 'up'"
                  [class.text-rose-700]="delta.direction === 'down'"
                  [class.dark:text-rose-300]="delta.direction === 'down'"
                  [class.text-slate-500]="delta.direction === 'stable'"
                >
                  @if (delta.direction === 'up') {
                    ↑ {{ delta.points }} {{ 'health.pointsFromLastWeek' | t }}
                  } @else if (delta.direction === 'down') {
                    ↓ {{ delta.points }} {{ 'health.pointsFromLastWeek' | t }}
                  } @else {
                    {{ 'health.noChangeFromLastWeek' | t }}
                  }
                </p>
              } @else {
                <p class="mt-2 text-xs stratix-muted">{{ 'health.trendBuilding' | t }}</p>
              }
            } @else {
              <p class="mt-3 text-sm stratix-muted">{{ 'health.unavailable' | t }}</p>
            }
          </div>

          <div class="min-w-[200px] flex-1 sm:max-w-xs">
            <p class="mb-1 text-[11px] font-semibold uppercase tracking-wide stratix-muted">
              {{ 'health.trend' | t }}
            </p>
            @if (sparkline(); as line) {
              <div class="relative rounded-xl border border-emerald-100 bg-white/80 px-3 py-2 dark:border-emerald-900/40 dark:bg-slate-900/50">
                <svg viewBox="0 0 200 64" class="h-16 w-full" role="img" [attr.aria-label]="'health.trend' | t">
                  <polyline
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2.5"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    class="text-emerald-600 dark:text-emerald-400"
                    [attr.points]="line.points"
                  />
                  @for (dot of line.dots; track dot.i) {
                    <circle [attr.cx]="dot.x" [attr.cy]="dot.y" r="2.5" class="fill-emerald-600 dark:fill-emerald-400" />
                    <text
                      [attr.x]="dot.labelX"
                      [attr.y]="dot.labelY"
                      class="fill-slate-500 text-[8px] dark:fill-slate-400"
                      text-anchor="end"
                    >
                      {{ dot.text }}
                    </text>
                  }
                </svg>
              </div>
            } @else {
              <div class="flex h-16 items-center justify-center rounded-xl border border-dashed border-slate-200 text-xs stratix-muted dark:border-slate-700">
                {{ 'health.trendEmpty' | t }}
              </div>
            }
          </div>
        </div>

        @if (health(); as h) {
          <div class="mt-6">
            <p class="mb-2 text-[11px] font-semibold uppercase tracking-wide stratix-muted">
              {{ 'health.breakdown' | t }}
            </p>
            <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
              <div class="rounded-xl border border-white/80 bg-white/90 px-4 py-3 shadow-sm dark:border-slate-700 dark:bg-slate-900/70">
                <p class="text-xs stratix-muted">{{ 'health.factorProgress' | t }}</p>
                <p class="mt-1 text-2xl font-bold tabular-nums text-dark dark:text-slate-100">
                  {{ MathRound(h.factors.progress) }}
                </p>
              </div>
              <div class="rounded-xl border border-white/80 bg-white/90 px-4 py-3 shadow-sm dark:border-slate-700 dark:bg-slate-900/70">
                <p class="text-xs stratix-muted">{{ 'health.onTimeDelivery' | t }}</p>
                <p class="mt-1 text-2xl font-bold tabular-nums text-emerald-700 dark:text-emerald-300">
                  {{ MathRound(h.factors.onTimeRate) }}
                </p>
              </div>
              <div class="rounded-xl border border-white/80 bg-white/90 px-4 py-3 shadow-sm dark:border-slate-700 dark:bg-slate-900/70">
                <p class="text-xs stratix-muted">{{ 'health.delayedWork' | t }}</p>
                <p class="mt-1 text-2xl font-bold tabular-nums text-rose-600 dark:text-rose-300">
                  {{ MathRound(h.factors.delayedRate) }}%
                </p>
              </div>
              <div class="rounded-xl border border-white/80 bg-white/90 px-4 py-3 shadow-sm dark:border-slate-700 dark:bg-slate-900/70">
                <p class="text-xs stratix-muted">{{ 'health.factorCriticalRisks' | t }}</p>
                <p
                  class="mt-1 text-2xl font-bold tabular-nums"
                  [class.text-rose-600]="h.factors.criticalRisks > 0"
                  [class.dark:text-rose-300]="h.factors.criticalRisks > 0"
                  [class.text-dark]="h.factors.criticalRisks === 0"
                  [class.dark:text-slate-100]="h.factors.criticalRisks === 0"
                >
                  {{ h.factors.criticalRisks }}
                </p>
              </div>
            </div>
          </div>
        }
      </div>
    </section>
  `,
})
export class ProjectHealthOverviewComponent {
  readonly projectId = input.required<number>();
  readonly health = input<ProjectHealthResult | null | undefined>();

  private readonly api = inject(ApiService);

  readonly snapshots = signal<ProjectHealthSnapshot[]>([]);
  readonly healthStatusClass = healthStatusClass;
  /** Expose Math.round for template without adding a pipe. */
  readonly MathRound = (n: number) => Math.round(n);

  readonly weekDelta = computed(() => {
    const h = this.health();
    if (!h) return null;
    return weekDeltaFromSnapshots(this.snapshots(), h.score);
  });

  readonly sparkline = computed(() => {
    const h = this.health();
    const points = trendPointsFromSnapshots(this.snapshots(), h?.score);
    if (points.length === 0) return null;
    const scores = points.map((p) => p.score);
    const width = 200;
    const height = 64;
    const pad = 10;
    const min = Math.min(...scores, 0);
    const max = Math.max(...scores, 100);
    const span = Math.max(1, max - min);
    const usableW = width - pad * 2;
    const usableH = height - pad * 2;

    const coords = scores.map((score, i) => {
      const x = scores.length === 1 ? width / 2 : pad + (i / (scores.length - 1)) * usableW;
      const y = pad + usableH - ((score - min) / span) * usableH;
      return { i, x, y, score };
    });

    const poly = coords.map((c) => `${c.x.toFixed(1)},${c.y.toFixed(1)}`).join(' ');
    const pick =
      coords.length <= 3
        ? coords
        : [coords[0], coords[Math.floor(coords.length / 2)], coords[coords.length - 1]];

    return {
      points: poly,
      dots: pick.map((c) => ({
        i: c.i,
        x: c.x,
        y: c.y,
        text: String(c.score),
        labelX: Math.max(14, c.x - 4),
        labelY: Math.max(10, c.y - 4),
      })),
    };
  });

  constructor() {
    effect((onCleanup) => {
      const id = this.projectId();
      if (!id) {
        this.snapshots.set([]);
        return;
      }
      const sub = this.loadSnapshots(id);
      onCleanup(() => sub.unsubscribe());
    });
  }

  private loadSnapshots(id: number) {
    return this.api.getHealthSnapshotHistory(id, 30).subscribe({
      next: (list) => this.snapshots.set(Array.isArray(list) ? list : []),
      error: () => this.snapshots.set([]),
    });
  }
}
