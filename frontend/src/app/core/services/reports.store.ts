import { Injectable, inject, signal } from '@angular/core';
import { ApiService } from './api.service';
import { CreateReportPayload, ReportResponse } from '../models/report.model';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ReportsStore {
  private readonly api = inject(ApiService);

  readonly reports = signal<ReportResponse[]>([]);
  readonly loaded = signal(false);
  readonly saving = signal(false);

  async loadFromApi(): Promise<void> {
    try {
      const rows = await firstValueFrom(this.api.getReports());
      this.reports.set(rows);
      this.loaded.set(true);
    } catch {
      this.reports.set([]);
      this.loaded.set(true);
    }
  }

  async saveExport(meta: CreateReportPayload, file: File): Promise<ReportResponse | null> {
    this.saving.set(true);
    try {
      const created = await firstValueFrom(this.api.createReport(meta, file));
      this.reports.update((list) => [created, ...list]);
      return created;
    } catch {
      return null;
    } finally {
      this.saving.set(false);
    }
  }

  async remove(id: number): Promise<boolean> {
    try {
      await firstValueFrom(this.api.deleteReport(id));
      this.reports.update((list) => list.filter((r) => r.id !== id));
      return true;
    } catch {
      return false;
    }
  }
}
