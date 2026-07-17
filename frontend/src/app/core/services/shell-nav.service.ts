import { Injectable, signal } from '@angular/core';

/** Shared shell nav state — mobile drawer open/close. */
@Injectable({ providedIn: 'root' })
export class ShellNavService {
  readonly open = signal(false);

  toggle(): void {
    this.open.update((v) => !v);
  }

  close(): void {
    this.open.set(false);
  }

  show(): void {
    this.open.set(true);
  }
}
