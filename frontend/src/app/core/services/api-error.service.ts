import { Injectable, signal } from '@angular/core';

export interface ApiErrorToast {
  id: number;
  messageKey: string;
  status: number;
  correlationId: string | null;
  detail?: string;
}

@Injectable({ providedIn: 'root' })
export class ApiErrorService {
  private seq = 0;
  private readonly _toast = signal<ApiErrorToast | null>(null);
  readonly toast = this._toast.asReadonly();

  publish(input: Omit<ApiErrorToast, 'id'>): void {
    this._toast.set({ ...input, id: ++this.seq });
  }

  dismiss(): void {
    this._toast.set(null);
  }
}
