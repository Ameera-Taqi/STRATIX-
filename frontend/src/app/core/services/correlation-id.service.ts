import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class CorrelationIdService {
  private readonly _last = signal<string | null>(null);
  readonly last = this._last.asReadonly();

  setLast(id: string): void {
    this._last.set(id);
  }
}
