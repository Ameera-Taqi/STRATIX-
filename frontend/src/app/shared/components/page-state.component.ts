import { Component, input, output } from '@angular/core';
import { TranslatePipe } from '../../core/i18n/translate.pipe';

/** Unified loading / empty / error shell for list pages. */
@Component({
  selector: 'app-page-state',
  standalone: true,
  imports: [TranslatePipe],
  template: `
    @if (loading()) {
      <div class="flex items-center justify-center py-16 text-sm text-slate-500" role="status">
        {{ loadingKey() | t }}
      </div>
    } @else if (errorKey()) {
      <div class="rounded-xl border border-rose-200 bg-rose-50 px-4 py-6 text-center dark:border-rose-900 dark:bg-rose-950/40" role="alert">
        <p class="text-sm font-medium text-rose-700 dark:text-rose-300">{{ errorKey()! | t }}</p>
        <button type="button" class="mt-3 text-sm font-medium text-primary underline" (click)="retry.emit()">
          {{ 'common.retry' | t }}
        </button>
      </div>
    } @else if (empty()) {
      <div class="py-16 text-center text-sm text-slate-500">
        {{ emptyKey() | t }}
      </div>
    } @else {
      <ng-content />
    }
  `,
})
export class PageStateComponent {
  readonly loading = input(false);
  readonly empty = input(false);
  readonly errorKey = input<string | null>(null);
  readonly loadingKey = input('common.loading');
  readonly emptyKey = input('common.empty');
  readonly retry = output<void>();
}
