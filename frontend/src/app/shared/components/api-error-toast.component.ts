import { Component, effect, inject, signal } from '@angular/core';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { ApiErrorService } from '../../core/services/api-error.service';

@Component({
  selector: 'app-api-error-toast',
  standalone: true,
  imports: [TranslatePipe],
  template: `
    @if (toast(); as t) {
      <div
        class="pointer-events-auto fixed bottom-4 end-4 z-[100] max-w-sm rounded-xl border border-rose-200 bg-white p-4 shadow-lg dark:border-rose-900 dark:bg-slate-900"
        role="alert"
        aria-live="assertive"
      >
        <div class="flex items-start justify-between gap-3">
          <div>
            <p class="text-sm font-semibold text-rose-700 dark:text-rose-300">{{ t.messageKey | t }}</p>
            @if (t.detail) {
              <p class="mt-1 text-xs text-slate-600 dark:text-slate-400">{{ t.detail }}</p>
            }
            @if (t.correlationId) {
              <p class="mt-2 font-mono text-[11px] text-slate-400">
                {{ 'errors.correlationId' | t }}: {{ t.correlationId }}
              </p>
            }
          </div>
          <button
            type="button"
            class="rounded-md p-1 text-slate-400 hover:bg-slate-100 hover:text-dark dark:hover:bg-slate-800"
            [attr.aria-label]="'common.close' | t"
            (click)="dismiss()"
          >
            ×
          </button>
        </div>
      </div>
    }
  `,
})
export class ApiErrorToastComponent {
  private readonly errors = inject(ApiErrorService);
  readonly toast = this.errors.toast;
  private readonly dismissTimer = signal<ReturnType<typeof setTimeout> | null>(null);

  constructor() {
    effect(() => {
      const current = this.toast();
      const prev = this.dismissTimer();
      if (prev) clearTimeout(prev);
      if (!current) return;
      this.dismissTimer.set(setTimeout(() => this.errors.dismiss(), 8000));
    });
  }

  dismiss(): void {
    this.errors.dismiss();
  }
}
