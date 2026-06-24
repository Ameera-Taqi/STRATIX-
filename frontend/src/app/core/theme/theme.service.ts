import { computed, Injectable, signal } from '@angular/core';

export type ThemeMode = 'light' | 'dark';

const STORAGE_KEY = 'stratix-theme';

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
    this.apply(mode);
  }

  toggle(): void {
    this.setMode(this.mode() === 'light' ? 'dark' : 'light');
  }

  private loadStored(): ThemeMode {
    const v = localStorage.getItem(STORAGE_KEY);
    if (v === 'dark' || v === 'light') return v;
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  private apply(mode: ThemeMode): void {
    document.documentElement.classList.toggle('dark', mode === 'dark');
  }
}
