import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { LanguageService } from '../../core/i18n/language.service';
import { ReportExportService } from '../../core/services/report-export.service';
import { ProjectsStore } from '../../core/services/projects.store';
import { TasksStore } from '../../core/services/tasks.store';
import { ReportsStore } from '../../core/services/reports.store';
import { ApiService } from '../../core/services/api.service';
import { ReportFormatCode, ReportResponse, ReportTypeCode } from '../../core/models/report.model';
import { downloadBlob, readBlobErrorMessage } from '../../shared/utils/blob-download.util';

function monthBounds(d = new Date()): { from: string; to: string } {
  const from = new Date(d.getFullYear(), d.getMonth(), 1);
  const to = new Date(d.getFullYear(), d.getMonth() + 1, 0);
  const iso = (x: Date) => x.toISOString().slice(0, 10);
  return { from: iso(from), to: iso(to) };
}

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [TopbarComponent, TranslatePipe, FormsModule, DatePipe],
  templateUrl: './reports.component.html',
})
export class ReportsComponent implements OnInit {
  private readonly exportService = inject(ReportExportService);
  private readonly lang = inject(LanguageService);
  private readonly projectsStore = inject(ProjectsStore);
  private readonly tasksStore = inject(TasksStore);
  readonly reportsStore = inject(ReportsStore);
  private readonly api = inject(ApiService);

  private readonly bounds = monthBounds();

  readonly reportType = signal<ReportTypeCode>('PROJECTS_PROGRESS');
  readonly projectId = signal<number | null>(null);
  readonly dateFrom = signal(this.bounds.from);
  readonly dateTo = signal(this.bounds.to);
  readonly format = signal<ReportFormatCode>('PDF');

  readonly generating = signal(false);
  readonly generateError = signal<string | null>(null);
  readonly lastGenerated = signal<ReportResponse | null>(null);

  readonly projects = this.projectsStore.projects;

  readonly reportTypes: { value: ReportTypeCode; labelKey: string }[] = [
    { value: 'PROJECTS_PROGRESS', labelKey: 'reports.type.PROJECTS_PROGRESS' },
    { value: 'TASKS_STATUS', labelKey: 'reports.type.TASKS_STATUS' },
    { value: 'EMPLOYEE_PERFORMANCE', labelKey: 'reports.type.EMPLOYEE_PERFORMANCE' },
    { value: 'DELAYED_TASKS', labelKey: 'reports.type.DELAYED_TASKS' },
    { value: 'KPI_SUMMARY', labelKey: 'reports.type.KPI_SUMMARY' },
  ];

  readonly formats: { value: ReportFormatCode; labelKey: string }[] = [
    { value: 'PDF', labelKey: 'reports.format.PDF' },
    { value: 'EXCEL', labelKey: 'reports.format.EXCEL' },
  ];

  ngOnInit(): void {
    if (!this.projectsStore.loaded()) {
      this.projectsStore.loadFromApi(() => this.tasksStore.loadFromApi());
    }
    void this.reportsStore.loadFromApi();
  }

  async generate(): Promise<void> {
    const from = this.dateFrom();
    const to = this.dateTo();
    if (!from || !to || from > to) {
      this.generateError.set('reports.errorPeriod');
      return;
    }

    this.generating.set(true);
    this.generateError.set(null);
    this.lastGenerated.set(null);

    try {
      const created = await this.exportService.generate({
        reportType: this.reportType(),
        format: this.format(),
        projectId: this.projectId(),
        dateFrom: from,
        dateTo: to,
      });
      if (!created) {
        this.generateError.set('reports.errorGenerate');
        return;
      }
      this.lastGenerated.set(created);
    } finally {
      this.generating.set(false);
    }
  }

  typeLabel(code: string): string {
    const key = `reports.type.${code}`;
    const translated = this.lang.t(key);
    return translated === key ? code : translated;
  }

  formatBytes(bytes: number): string {
    if (!bytes || bytes < 0) return '—';
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  async downloadSaved(id: number, fileName: string): Promise<void> {
    try {
      const blob = await firstValueFrom(this.api.downloadReport(id));
      if (blob.type.includes('json')) {
        const message = await readBlobErrorMessage(blob);
        throw new Error(message ?? 'Download failed');
      }
      downloadBlob(blob, fileName || `report-${id}`);
    } catch {
      /* ApiErrorInterceptor surfaces server failures */
    }
  }

  async openSaved(id: number, fileName: string): Promise<void> {
    try {
      const blob = await firstValueFrom(this.api.downloadReport(id));
      if (blob.type.includes('json')) {
        const message = await readBlobErrorMessage(blob);
        throw new Error(message ?? 'Open failed');
      }
      const url = URL.createObjectURL(blob);
      const win = window.open(url, '_blank');
      if (!win) {
        downloadBlob(blob, fileName || `report-${id}`);
      }
      setTimeout(() => URL.revokeObjectURL(url), 60_000);
    } catch {
      /* ApiErrorInterceptor surfaces server failures */
    }
  }

  async removeSaved(id: number): Promise<void> {
    await this.reportsStore.remove(id);
    if (this.lastGenerated()?.id === id) this.lastGenerated.set(null);
  }
}
