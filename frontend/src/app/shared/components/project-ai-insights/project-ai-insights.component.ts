import { Component, inject, input, signal } from '@angular/core';
import { ProjectHealthAnalysisResponse } from '../../../core/models/ai-health-analysis.model';
import { ApiService } from '../../../core/services/api.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { healthStatusClass } from '../../utils/project-health.util';
import { UiIconComponent } from '../ui-icon/ui-icon.component';

@Component({
  selector: 'app-project-ai-insights',
  standalone: true,
  imports: [TranslatePipe, UiIconComponent],
  template: `
    <div [class]="embedded() ? 'p-4' : 'stratix-card overflow-hidden border border-violet-200 dark:border-violet-900/40'">
      @if (!embedded()) {
        <div class="flex flex-wrap items-center justify-between gap-3 border-b border-violet-100 bg-gradient-to-r from-violet-50 to-primary/5 px-6 py-4 dark:border-violet-900/30 dark:from-violet-950/40 dark:to-primary/10">
          <div class="flex items-center gap-3">
            <span class="flex h-10 w-10 items-center justify-center rounded-xl bg-violet-600 text-white shadow-sm">
              <app-ui-icon name="sparkles" size="md" />
            </span>
            <div>
              <h3 class="stratix-heading text-base">{{ 'ai.title' | t }}</h3>
              <p class="text-xs stratix-muted">{{ 'ai.subtitle' | t }}</p>
            </div>
          </div>
          <button
            type="button"
            class="rounded-lg bg-violet-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition hover:bg-violet-700 disabled:opacity-60"
            [disabled]="loading()"
            (click)="analyze()"
          >
            @if (loading()) {
              {{ 'ai.analyzing' | t }}
            } @else {
              {{ 'ai.analyze' | t }}
            }
          </button>
        </div>
      }

      <div [class.p-6]="!embedded()" [class.p-0]="embedded()">
        @if (embedded()) {
          <button
            type="button"
            class="mb-4 w-full rounded-lg bg-violet-600 px-4 py-2.5 text-sm font-medium text-white shadow-sm transition hover:bg-violet-700 disabled:opacity-60"
            [disabled]="loading()"
            (click)="analyze()"
          >
            @if (loading()) {
              {{ 'ai.analyzing' | t }}
            } @else {
              {{ 'ai.analyze' | t }}
            }
          </button>
        }

        @if (error()) {
          <div class="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700 dark:border-red-900/40 dark:bg-red-950/30 dark:text-red-300">
            <p class="font-medium">{{ 'ai.errorTitle' | t }}</p>
            <p class="mt-1">{{ error() }}</p>
            <p class="mt-2 text-xs opacity-80">{{ 'ai.errorHint' | t }}</p>
          </div>
        } @else if (!result()) {
          <div class="rounded-lg border border-dashed border-violet-200 bg-violet-50/50 px-4 py-8 text-center dark:border-violet-900/40 dark:bg-violet-950/20">
            <p class="flex justify-center text-violet-600"><app-ui-icon name="bot" size="xl" /></p>
            <p class="mt-2 text-sm font-medium text-dark dark:text-slate-100">{{ 'ai.emptyTitle' | t }}</p>
            <p class="mx-auto mt-1 text-xs stratix-muted">{{ 'ai.botEmptyHint' | t }}</p>
          </div>
        } @else {
          <div class="mb-4 flex flex-wrap gap-2">
            <span class="stratix-badge px-2 py-1 text-[10px]" [class]="healthStatusClass(result()!.healthStatus)">
              {{ 'ai.healthLabel' | t }}: {{ result()!.healthStatus }}
            </span>
            <span class="stratix-badge px-2 py-1 text-[10px]" [class]="deliveryRiskClass(result()!.deliveryRisk)">
              {{ 'ai.deliveryRisk' | t }}: {{ result()!.deliveryRisk }}
            </span>
            @if (result()!.analysisEngine === 'OPENAI') {
              <span class="stratix-badge bg-violet-100 px-2 py-1 text-[10px] text-violet-700 dark:bg-violet-900/40 dark:text-violet-300">
                {{ 'ai.engineOpenai' | t }}
              </span>
            } @else {
              <span class="stratix-badge bg-slate-100 px-2 py-1 text-[10px] text-slate-600 dark:bg-slate-800 dark:text-slate-300">
                {{ 'ai.engineLocal' | t }}
              </span>
            }
          </div>

          <section class="mb-4">
            <h4 class="mb-1.5 text-xs font-semibold text-dark dark:text-slate-100">{{ 'ai.executiveSummary' | t }}</h4>
            <p class="rounded-lg bg-slate-50 px-3 py-2 text-xs leading-relaxed text-slate-700 dark:bg-slate-800/60 dark:text-slate-300">
              {{ result()!.executiveSummary }}
            </p>
          </section>

          <section class="mb-4">
            <h4 class="mb-2 flex items-center gap-1.5 text-xs font-semibold text-dark dark:text-slate-100">
              <app-ui-icon name="warning" size="xs" className="text-amber-500" /> {{ 'ai.mainConcerns' | t }}
            </h4>
            <ul class="space-y-1.5">
              @for (item of result()!.mainConcerns; track item) {
                <li class="rounded-lg border border-amber-100 bg-amber-50 px-2.5 py-1.5 text-[11px] text-amber-900 dark:border-amber-900/30 dark:bg-amber-950/20 dark:text-amber-200">
                  {{ item }}
                </li>
              } @empty {
                <li class="text-[11px] stratix-muted">{{ 'ai.none' | t }}</li>
              }
            </ul>
          </section>

          <section class="mb-4">
            <h4 class="mb-2 flex items-center gap-1.5 text-xs font-semibold text-dark dark:text-slate-100">
              <app-ui-icon name="check" size="xs" className="text-primary" /> {{ 'ai.recommendations' | t }}
            </h4>
            <ul class="space-y-1.5">
              @for (item of result()!.recommendations; track item) {
                <li class="rounded-lg border border-primary/20 bg-primary/5 px-2.5 py-1.5 text-[11px] text-slate-700 dark:text-slate-300">
                  {{ item }}
                </li>
              } @empty {
                <li class="text-[11px] stratix-muted">{{ 'ai.none' | t }}</li>
              }
            </ul>
          </section>

          <section>
            <h4 class="mb-2 flex items-center gap-1.5 text-xs font-semibold text-dark dark:text-slate-100">
              <app-ui-icon name="lightbulb" size="xs" className="text-violet-500" /> {{ 'ai.managementInsights' | t }}
            </h4>
            <ul class="space-y-1.5">
              @for (item of result()!.managementInsights; track item) {
                <li class="rounded-lg border border-violet-100 bg-violet-50 px-2.5 py-1.5 text-[11px] text-violet-900 dark:border-violet-900/30 dark:bg-violet-950/20 dark:text-violet-200">
                  {{ item }}
                </li>
              } @empty {
                <li class="text-[11px] stratix-muted">{{ 'ai.none' | t }}</li>
              }
            </ul>
          </section>
        }
      </div>
    </div>
  `,
})
export class ProjectAiInsightsComponent {
  readonly projectId = input.required<number>();
  readonly embedded = input(false);

  private readonly api = inject(ApiService);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly result = signal<ProjectHealthAnalysisResponse | null>(null);

  readonly healthStatusClass = healthStatusClass;

  deliveryRiskClass(risk: string): string {
    const map: Record<string, string> = {
      LOW: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300',
      MEDIUM: 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300',
      HIGH: 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300',
    };
    return map[risk] ?? map['MEDIUM'];
  }

  analyze(): void {
    const projectId = this.projectId();
    if (!projectId) {
      this.error.set('Project not found');
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.api.analyzeProjectHealth({ projectId }).subscribe({
      next: (response) => {
        this.result.set(response);
        this.loading.set(false);
      },
      error: (err) => {
        const detail = err?.error?.detail ?? err?.message ?? 'Unknown error';
        this.error.set(typeof detail === 'string' ? detail : 'AI analysis failed');
        this.loading.set(false);
      },
    });
  }
}
