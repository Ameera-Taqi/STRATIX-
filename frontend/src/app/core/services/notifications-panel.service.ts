import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class NotificationsPanelService {
  private readonly _open = signal(false);

  readonly open = this._open.asReadonly();

  toggle(): void {
    this._open.update((v) => !v);
  }

  close(): void {
    this._open.set(false);
  }
}
