import { Component, input, signal } from '@angular/core';
import { ProjectAiInsightsComponent } from '../project-ai-insights/project-ai-insights.component';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

@Component({
  selector: 'app-project-ai-bot',
  standalone: true,
  imports: [ProjectAiInsightsComponent, TranslatePipe],
  template: `
    @if (open()) {
      <div
        class="fixed bottom-24 end-6 z-50 flex w-[min(100vw-2rem,420px)] max-h-[min(80vh,640px)] flex-col overflow-hidden rounded-2xl border border-violet-200 bg-white shadow-2xl dark:border-violet-800 dark:bg-slate-900"
        role="dialog"
        [attr.aria-label]="'ai.title' | t"
      >
        <div class="flex items-center justify-between gap-2 border-b border-violet-100 bg-gradient-to-r from-violet-600 to-primary px-4 py-3 text-white dark:border-violet-900">
          <div class="flex items-center gap-2">
            <span class="flex h-8 w-8 items-center justify-center rounded-full bg-white/20 text-sm">🤖</span>
            <div>
              <p class="text-sm font-semibold leading-tight">{{ 'ai.botName' | t }}</p>
              <p class="text-[10px] text-white/80">{{ 'ai.subtitle' | t }}</p>
            </div>
          </div>
          <button
            type="button"
            class="rounded-lg p-1.5 text-white/90 transition hover:bg-white/20"
            [attr.aria-label]="'ai.close' | t"
            (click)="open.set(false)"
          >
            ✕
          </button>
        </div>
        <div class="min-h-0 flex-1 overflow-y-auto">
          <app-project-ai-insights [projectId]="projectId()" [embedded]="true" />
        </div>
      </div>
    }

    <button
      type="button"
      class="fixed bottom-6 end-6 z-50 flex h-14 w-14 items-center justify-center rounded-full bg-violet-600 text-2xl text-white shadow-lg ring-4 ring-violet-200 transition hover:scale-105 hover:bg-violet-700 active:scale-95 dark:ring-violet-900/50"
      [class.rotate-0]="!open()"
      [attr.aria-expanded]="open()"
      [attr.aria-label]="'ai.openBot' | t"
      (click)="toggle()"
    >
      @if (open()) {
        ✕
      } @else {
        🤖
      }
    </button>
  `,
})
export class ProjectAiBotComponent {
  readonly projectId = input.required<number>();
  readonly open = signal(false);

  toggle(): void {
    this.open.update((v) => !v);
  }
}
