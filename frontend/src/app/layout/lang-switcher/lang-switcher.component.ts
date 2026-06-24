import { Component, inject } from '@angular/core';
import { LanguageService } from '../../core/i18n/language.service';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { Lang } from '../../core/i18n/translations';

@Component({
  selector: 'app-lang-switcher',
  standalone: true,
  imports: [TranslatePipe],
  template: `
    <div
      class="flex rounded-lg border border-slate-200 bg-surface p-0.5 text-xs font-semibold dark:border-slate-600 dark:bg-slate-800"
      role="group"
      [attr.aria-label]="'lang.switch' | t"
    >
      @for (opt of options; track opt.code) {
        <button
          type="button"
          class="rounded-md px-2.5 py-1.5 transition"
          [class.bg-primary]="i18n.lang() === opt.code"
          [class.text-white]="i18n.lang() === opt.code"
          [class.text-slate-600]="i18n.lang() !== opt.code"
          [class.dark:text-slate-300]="i18n.lang() !== opt.code"
          (click)="select(opt.code)"
        >
          {{ opt.labelKey | t }}
        </button>
      }
    </div>
  `,
})
export class LangSwitcherComponent {
  readonly i18n = inject(LanguageService);
  readonly options: { code: Lang; labelKey: string }[] = [
    { code: 'en', labelKey: 'lang.en' },
    { code: 'ar', labelKey: 'lang.ar' },
  ];

  select(lang: Lang): void {
    this.i18n.setLang(lang);
  }
}
