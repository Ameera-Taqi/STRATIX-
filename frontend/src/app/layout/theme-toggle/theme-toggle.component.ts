import { Component, inject } from '@angular/core';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { ThemeService } from '../../core/theme/theme.service';

@Component({
  selector: 'app-theme-toggle',
  standalone: true,
  imports: [TranslatePipe],
  template: `
    <button
      type="button"
      class="rounded-lg border border-slate-200 bg-surface p-2 text-lg leading-none text-slate-600 transition hover:bg-slate-100 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-300 dark:hover:bg-slate-700"
      (click)="theme.toggle()"
      [attr.aria-label]="(theme.isDark() ? 'theme.light' : 'theme.dark') | t"
      [title]="(theme.isDark() ? 'theme.light' : 'theme.dark') | t"
    >
      {{ theme.isDark() ? '☀️' : '🌙' }}
    </button>
  `,
})
export class ThemeToggleComponent {
  readonly theme = inject(ThemeService);
}
