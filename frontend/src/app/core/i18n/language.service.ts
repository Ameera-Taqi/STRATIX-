import { computed, Injectable, signal } from '@angular/core';
import { Lang, TRANSLATIONS, TranslationKey } from './translations';

const STORAGE_KEY = 'stratix-lang';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  readonly lang = signal<Lang>(this.loadStored());
  readonly isRtl = computed(() => this.lang() === 'ar');

  constructor() {
    this.applyDocument(this.lang());
  }

  t(key: TranslationKey | string): string {
    const lang = this.lang();
    const table = TRANSLATIONS[lang] as Record<string, string>;
    return table[key] ?? TRANSLATIONS.en[key as TranslationKey] ?? key;
  }

  setLang(lang: Lang): void {
    this.lang.set(lang);
    localStorage.setItem(STORAGE_KEY, lang);
    this.applyDocument(lang);
  }

  toggle(): void {
    this.setLang(this.lang() === 'en' ? 'ar' : 'en');
  }

  private loadStored(): Lang {
    const v = localStorage.getItem(STORAGE_KEY);
    return v === 'ar' || v === 'en' ? v : 'en';
  }

  private applyDocument(lang: Lang): void {
    const html = document.documentElement;
    html.lang = lang;
    html.dir = lang === 'ar' ? 'rtl' : 'ltr';
  }
}
