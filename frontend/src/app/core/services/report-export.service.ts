import { Injectable, inject } from '@angular/core';
import { AuditLogStore } from './audit-log.store';
import { ReportsStore } from './reports.store';
import { GenerateReportPayload, ReportFormatCode, ReportResponse, ReportTypeCode } from '../models/report.model';

export interface GenerateReportOptions {
  reportType: ReportTypeCode | string;
  format: ReportFormatCode;
  projectId: number | null;
  dateFrom: string;
  dateTo: string;
  departmentId?: number | null;
  employeeId?: number | null;
  title?: string | null;
}

/**
 * Triggers server-side report generation (authoritative data on the API).
 * Client-side HTML/CSV builders were removed — the backend owns PDF/XLSX output.
 */
@Injectable({ providedIn: 'root' })
export class ReportExportService {
  private readonly audit = inject(AuditLogStore);
  private readonly reportsStore = inject(ReportsStore);

  async generate(options: GenerateReportOptions): Promise<ReportResponse | null> {
    const payload: GenerateReportPayload = {
      title: options.title ?? null,
      reportType: options.reportType,
      format: options.format,
      projectId: options.projectId,
      departmentId: options.departmentId ?? null,
      employeeId: options.employeeId ?? null,
      dateFrom: options.dateFrom,
      dateTo: options.dateTo,
    };

    const created = await this.reportsStore.generate(payload);
    if (created) {
      this.audit.log({
        entityType: 'REPORT',
        entityId: created.id,
        entityLabel: created.title,
        action: 'EXPORT',
        details: `Report generated (${options.reportType} / ${options.format})`,
      });
    }
    return created;
  }

  /** Convenience: current-month projects progress PDF. */
  exportExecutivePdf(options?: { projectId?: number | null; employeeId?: number | null }): void {
    const now = new Date();
    const from = new Date(now.getFullYear(), now.getMonth(), 1);
    const to = new Date(now.getFullYear(), now.getMonth() + 1, 0);
    void this.generate({
      reportType: 'PROJECTS_PROGRESS',
      format: 'PDF',
      projectId: options?.projectId ?? null,
      employeeId: options?.employeeId ?? null,
      dateFrom: from.toISOString().slice(0, 10),
      dateTo: to.toISOString().slice(0, 10),
    });
  }
}
