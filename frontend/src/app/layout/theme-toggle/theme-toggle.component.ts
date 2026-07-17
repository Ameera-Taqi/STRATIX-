import { Component, inject } from '@angular/core';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { ThemeService } from '../../core/theme/theme.service';
import { UiIconComponent } from '../../shared/components/ui-icon/ui-icon.component';

@Component({
  selector: 'app-theme-toggle',
  standalone: true,
  imports: [TranslatePipe, UiIconComponent],
  template: `
    <button
      type="button"
      class="rounded-lg border border-slate-200 bg-surface p-2 text-slate-600 transition hover:bg-slate-100 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-300 dark:hover:bg-slate-700"
      (click)="theme.toggle()"
      [attr.aria-label]="(theme.isDark() ? 'theme.light' : 'theme.dark') | t"
      [title]="(theme.isDark() ? 'theme.light' : 'theme.dark') | t"
    >
      @if (theme.isDark()) {
        <app-ui-icon name="sun" size="md" />
      } @else {
        <app-ui-icon name="moon" size="md" />
      }
    </button>
  `,
})
export class ThemeToggleComponent {
  readonly theme = inject(ThemeService);
}
