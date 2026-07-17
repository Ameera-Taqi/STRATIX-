import { computed, Injectable, signal } from '@angular/core';

export type ThemeMode = 'light' | 'dark';

/** Current storage key — bumped so legacy `stratix-theme=light` no longer blocks dark default. */
const STORAGE_KEY = 'stratix-theme-mode';
const LEGACY_KEY = 'stratix-theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly mode = signal<ThemeMode>(this.loadStored());
  readonly isDark = computed(() => this.mode() === 'dark');

  constructor() {
    this.apply(this.mode());
  }

  setMode(mode: ThemeMode): void {
    this.mode.set(mode);
    localStorage.setItem(STORAGE_KEY, mode);
    localStorage.removeItem(LEGACY_KEY);
    this.apply(mode);
  }

  toggle(): void {
    this.setMode(this.mode() === 'light' ? 'dark' : 'light');
  }

  private loadStored(): ThemeMode {
    // Drop the old key so a previous "light" choice cannot override the new default.
    try {
      localStorage.removeItem(LEGACY_KEY);
    } catch {
      /* ignore */
    }

    try {
      const v = localStorage.getItem(STORAGE_KEY);
      if (v === 'dark' || v === 'light') return v;
      localStorage.setItem(STORAGE_KEY, 'dark');
    } catch {
      /* ignore */
    }
    return 'dark';
  }

  private apply(mode: ThemeMode): void {
    const root = document.documentElement;
    root.classList.toggle('dark', mode === 'dark');
    root.style.colorScheme = mode;
  }
}
