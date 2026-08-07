import { NgClass } from '@angular/common';
import { Component, inject, input, output, signal } from '@angular/core';
import { ProjectHealthAnalysisResponse } from '../../../core/models/ai-health-analysis.model';
import { ApiService } from '../../../core/services/api.service';
import { LanguageService } from '../../../core/i18n/language.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { UiIconComponent } from '../ui-icon/ui-icon.component';

@Component({
  selector: 'app-project-ai-insights',
  standalone: true,
  imports: [NgClass, TranslatePipe, UiIconComponent],
  template: `
    <div
      [class]="
        embedded()
          ? ''
          : 'stratix-card overflow-hidden border border-violet-200 dark:border-violet-900/40'
      "
    >
      <div
        class="flex flex-wrap items-center justify-between gap-3"
        [ngClass]="
          embedded()
            ? 'mb-4'
            : 'border-b border-violet-100 bg-gradient-to-r from-violet-50 to-primary/5 px-6 py-4 dark:border-violet-900/30 dark:from-violet-950/40 dark:to-primary/10'
        "
      >
        <div class="flex items-center gap-3">
          @if (!embedded()) {
            <span class="flex h-10 w-10 items-center justify-center rounded-xl bg-violet-600 text-white shadow-sm">
              <app-ui-icon name="sparkles" size="md" />
            </span>
          }
          <div>
            <h3 class="text-sm font-bold tracking-tight text-dark dark:text-slate-100">
              {{ 'ai.projectAnalysis' | t }}
            </h3>
            <p class="text-xs stratix-muted">{{ 'ai.subtitleShort' | t }}</p>
          </div>
        </div>
        <button
          type="button"
          class="rounded-lg bg-violet-600 px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-violet-700 disabled:opacity-60"
          [disabled]="loading()"
          (click)="analyze()"
        >
          @if (loading()) {
            {{ 'ai.analyzing' | t }}
          } @else if (result()) {
            {{ 'ai.reanalyze' | t }}
          } @else {
            {{ 'ai.analyze' | t }}
          }
        </button>
      </div>

      <div [class.p-6]="!embedded()" [class.pt-0]="!embedded()">
        @if (error()) {
          <div
            class="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700 dark:border-red-900/40 dark:bg-red-950/30 dark:text-red-300"
          >
            <p class="font-medium">{{ 'ai.errorTitle' | t }}</p>
            <p class="mt-1">{{ error() }}</p>
            <p class="mt-2 text-xs opacity-80">{{ 'ai.errorHint' | t }}</p>
          </div>
        } @else if (!result()) {
          <div
            class="rounded-xl border border-dashed border-violet-200 bg-violet-50/50 px-4 py-10 text-center dark:border-violet-900/40 dark:bg-violet-950/20"
          >
            <p class="flex justify-center text-violet-600"><app-ui-icon name="bot" size="xl" /></p>
            <p class="mt-2 text-sm font-medium text-dark dark:text-slate-100">{{ 'ai.emptyTitle' | t }}</p>
            <p class="mx-auto mt-1 max-w-sm text-xs stratix-muted">{{ 'ai.botEmptyHint' | t }}</p>
          </div>
        } @else {
          <p class="mb-5 text-xs stratix-muted">
            {{ 'ai.basedOnDataAsOf' | t }}
            <span class="font-semibold text-slate-700 dark:text-slate-200">{{ analyzedAtLabel() }}</span>
          </p>

          <!-- Overall Assessment -->
          <section class="mb-6">
            <h4 class="text-xs font-bold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">
              {{ 'ai.overallAssessment' | t }}
            </h4>
            <div class="mt-2 border-t border-slate-200 pt-3 dark:border-slate-700">
              <p class="text-sm leading-relaxed text-slate-700 dark:text-slate-200">
                {{ result()!.executiveSummary }}
              </p>
            </div>
          </section>

          <!-- Main Concerns -->
          <section class="mb-6">
            <h4 class="text-xs font-bold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">
              {{ 'ai.mainConcerns' | t }}
            </h4>
            <div class="mt-2 border-t border-slate-200 pt-3 dark:border-slate-700">
              @if (result()!.mainConcerns.length === 0) {
                <p class="text-sm stratix-muted">{{ 'ai.none' | t }}</p>
              } @else {
                <ul class="space-y-2">
                  @for (item of result()!.mainConcerns; track item) {
                    <li class="flex gap-2 text-sm text-amber-900 dark:text-amber-200">
                      <span class="mt-0.5 shrink-0 font-bold text-amber-500" aria-hidden="true">⚠</span>
                      <span>{{ item }}</span>
                    </li>
                  }
                </ul>
              }
            </div>
          </section>

          <!-- Recommended Actions -->
          <section class="mb-6">
            <h4 class="text-xs font-bold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">
              {{ 'ai.recommendedActions' | t }}
            </h4>
            <div class="mt-2 border-t border-slate-200 pt-3 dark:border-slate-700">
              @if (result()!.recommendations.length === 0) {
                <p class="text-sm stratix-muted">{{ 'ai.none' | t }}</p>
              } @else {
                <ol class="space-y-3">
                  @for (item of result()!.recommendations; track $index; let i = $index) {
                    <li class="rounded-xl border border-slate-200 bg-white px-3 py-3 dark:border-slate-700 dark:bg-slate-900/40">
                      <div class="flex flex-wrap items-start justify-between gap-2">
                        <p class="min-w-0 flex-1 text-sm text-slate-800 dark:text-slate-100">
                          <span class="mr-1.5 font-bold tabular-nums text-violet-600 dark:text-violet-300">{{ i + 1 }}.</span>
                          {{ item }}
                        </p>
                        <button
                          type="button"
                          class="shrink-0 rounded-lg border border-violet-200 px-2.5 py-1 text-xs font-semibold text-violet-700 transition hover:bg-violet-50 dark:border-violet-800 dark:text-violet-200 dark:hover:bg-violet-950/40"
                          (click)="toggleReview(i)"
                        >
                          {{ reviewingIndex() === i ? ('ai.hideRecommendation' | t) : ('ai.reviewRecommendation' | t) }}
                        </button>
                      </div>
                      @if (reviewingIndex() === i) {
                        <div
                          class="mt-3 rounded-lg border border-violet-100 bg-violet-50/80 px-3 py-3 dark:border-violet-900/40 dark:bg-violet-950/30"
                        >
                          <p class="text-xs leading-relaxed text-violet-900 dark:text-violet-100">
                            {{ 'ai.reviewHint' | t }}
                          </p>
                          <p class="mt-2 text-sm font-medium text-dark dark:text-slate-100">{{ item }}</p>
                          <div class="mt-3 flex flex-wrap gap-2">
                            <button
                              type="button"
                              class="rounded-lg bg-violet-600 px-3 py-1.5 text-xs font-semibold text-white"
                              (click)="acknowledgeRecommendation(i)"
                            >
                              {{ 'ai.acknowledgeRecommendation' | t }}
                            </button>
                            <button
                              type="button"
                              class="rounded-lg px-3 py-1.5 text-xs font-medium text-slate-600 hover:underline dark:text-slate-300"
                              (click)="toggleReview(null)"
                            >
                              {{ 'common.dismiss' | t }}
                            </button>
                          </div>
                        </div>
                      }
                    </li>
                  }
                </ol>
              }
            </div>
          </section>

          <!-- Delivery Risk -->
          <section>
            <h4 class="text-xs font-bold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">
              {{ 'ai.deliveryRisk' | t }}
            </h4>
            <div class="mt-2 border-t border-slate-200 pt-3 dark:border-slate-700">
              <span
                class="inline-flex items-center rounded-full px-3 py-1 text-sm font-bold uppercase tracking-wide"
                [class]="deliveryRiskClass(result()!.deliveryRisk)"
              >
                {{ result()!.deliveryRisk }}
              </span>
            </div>
          </section>
        }
      </div>
    </div>
  `,
})
export class ProjectAiInsightsComponent {
  readonly projectId = input.required<number>();
  readonly embedded = input(false);
  readonly analyzed = output<void>();

  private readonly api = inject(ApiService);
  private readonly lang = inject(LanguageService);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly result = signal<ProjectHealthAnalysisResponse | null>(null);
  readonly reviewingIndex = signal<number | null>(null);
  readonly acknowledged = signal<Set<number>>(new Set());

  deliveryRiskClass(risk: string): string {
    const map: Record<string, string> = {
      LOW: 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-200',
      MEDIUM: 'bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-200',
      HIGH: 'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-200',
    };
    return map[risk] ?? map['MEDIUM'];
  }

  analyzedAtLabel(): string {
    const raw = this.result()?.analyzedAt;
    const d = raw ? new Date(raw) : new Date();
    if (Number.isNaN(d.getTime())) return '—';
    const locale = this.lang.lang() === 'ar' ? 'ar' : 'en-GB';
    return d.toLocaleString(locale, {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
    });
  }

  toggleReview(index: number | null): void {
    if (index == null) {
      this.reviewingIndex.set(null);
      return;
    }
    this.reviewingIndex.update((current) => (current === index ? null : index));
  }

  acknowledgeRecommendation(index: number): void {
    this.acknowledged.update((set) => {
      const next = new Set(set);
      next.add(index);
      return next;
    });
    this.reviewingIndex.set(null);
  }

  analyze(): void {
    const projectId = this.projectId();
    if (!projectId) {
      this.error.set('Project not found');
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.reviewingIndex.set(null);
    this.acknowledged.set(new Set());

    this.api.analyzeProjectHealth({ projectId }).subscribe({
      next: (response) => {
        this.result.set({
          ...response,
          analyzedAt: response.analyzedAt || new Date().toISOString(),
        });
        this.loading.set(false);
        this.analyzed.emit();
      },
      error: (err) => {
        const detail = err?.error?.detail ?? err?.message ?? 'Unknown error';
        this.error.set(typeof detail === 'string' ? detail : 'AI analysis failed');
        this.loading.set(false);
      },
    });
  }
}
