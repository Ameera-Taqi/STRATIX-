import { Injectable, inject } from '@angular/core';
import { LanguageService } from '../i18n/language.service';
import { AuditLogStore } from './audit-log.store';
import { ProjectsStore } from './projects.store';
import { TasksStore } from './tasks.store';
import { RisksStore } from './risks.store';
import { ProjectHealthService } from './project-health.service';
import { ReportsStore } from './reports.store';
import { EmployeesStore } from './employees.store';
import { CreateReportPayload, ReportFormatCode, ReportResponse, ReportTypeCode } from '../models/report.model';
import { computeScheduleStatus } from '../../shared/utils/dashboard-insights.util';

export interface GenerateReportOptions {
  reportType: ReportTypeCode | string;
  format: ReportFormatCode;
  projectId: number | null;
  dateFrom: string;
  dateTo: string;
}

@Injectable({ providedIn: 'root' })
export class ReportExportService {
  private readonly lang = inject(LanguageService);
  private readonly audit = inject(AuditLogStore);
  private readonly projects = inject(ProjectsStore);
  private readonly tasks = inject(TasksStore);
  private readonly risks = inject(RisksStore);
  private readonly health = inject(ProjectHealthService);
  private readonly reportsStore = inject(ReportsStore);
  private readonly employees = inject(EmployeesStore);

  async generate(options: GenerateReportOptions): Promise<ReportResponse | null> {
    const t = (k: string) => this.lang.t(k);
    const title = this.titleFor(options.reportType);
    const projectName =
      options.projectId != null
        ? (this.projects.projects().find((p) => p.id === options.projectId)?.name ?? '')
        : t('common.all');

    if (options.format === 'PDF') {
      const html = this.buildHtml(options, title, projectName);
      const file = new File([html], `${this.slug(title)}-${Date.now()}.html`, { type: 'text/html' });
      const created = await this.persist(file, {
        title,
        reportType: options.reportType,
        format: 'PDF',
        projectId: options.projectId,
        dateFrom: options.dateFrom,
        dateTo: options.dateTo,
      });
      this.audit.log({
        entityType: 'REPORT',
        entityId: created?.id ?? 0,
        entityLabel: title,
        action: 'EXPORT',
        details: `Report generated (${options.reportType} / PDF)`,
      });
      return created;
    }

    const csv = this.buildCsv(options);
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8' });
    const file = new File([blob], `${this.slug(title)}-${Date.now()}.csv`, { type: 'text/csv' });
    const created = await this.persist(file, {
      title,
      reportType: options.reportType,
      format: 'EXCEL',
      projectId: options.projectId,
      dateFrom: options.dateFrom,
      dateTo: options.dateTo,
    });
    this.audit.log({
      entityType: 'REPORT',
      entityId: created?.id ?? 0,
      entityLabel: title,
      action: 'EXPORT',
      details: `Report generated (${options.reportType} / EXCEL)`,
    });
    return created;
  }

  /** @deprecated Prefer generate() */
  exportExecutivePdf(options?: { projectId?: number | null; employeeId?: number | null }): void {
    const now = new Date();
    const from = new Date(now.getFullYear(), now.getMonth(), 1);
    const to = new Date(now.getFullYear(), now.getMonth() + 1, 0);
    void this.generate({
      reportType: 'PROJECTS_PROGRESS',
      format: 'PDF',
      projectId: options?.projectId ?? null,
      dateFrom: from.toISOString().slice(0, 10),
      dateTo: to.toISOString().slice(0, 10),
    });
  }

  private titleFor(type: string): string {
    const key = `reports.type.${type}`;
    const translated = this.lang.t(key);
    return translated === key ? this.lang.t('reports.executiveTitle') : translated;
  }

  private slug(title: string): string {
    return title
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-|-$/g, '')
      .slice(0, 40) || 'report';
  }

  private filteredProjectIds(projectId: number | null): Set<number> | null {
    if (projectId == null) return null;
    return new Set([projectId]);
  }

  private tasksInPeriod(projectId: number | null, dateFrom: string, dateTo: string) {
    const ids = this.filteredProjectIds(projectId);
    return this.tasks.getAll().filter((tk) => {
      if (ids && !ids.has(tk.projectId)) return false;
      return tk.dueDate >= dateFrom && tk.dueDate <= dateTo;
    });
  }

  private buildHtml(options: GenerateReportOptions, title: string, projectName: string): string {
    const t = (k: string) => this.lang.t(k);
    const generatedAt = new Date().toLocaleString();
    const ids = this.filteredProjectIds(options.projectId);
    const projects = this.projects.projects().filter((p) => !ids || ids.has(p.id));
    const healthRows = this.health
      .all()
      .filter((p) => !ids || ids.has(p.projectId))
      .map((p) => `<tr><td>${esc(p.projectName)}</td><td>${p.score}%</td><td>${p.status}</td><td>${p.progress}%</td></tr>`)
      .join('');
    const schedule = computeScheduleStatus(projects);
    const budgetRows = schedule
      .map(
        (b) =>
          `<tr><td>${esc(b.projectName)}</td><td>${Math.round(b.budget / 1000)}%</td><td>${Math.round(b.spent / 1000)}%</td><td>${b.status}</td></tr>`,
      )
      .join('');
    const tasks = this.tasksInPeriod(options.projectId, options.dateFrom, options.dateTo);
    const overdue = tasks.filter((tk) => tk.dueDate < new Date().toISOString().slice(0, 10) && tk.status !== 'DONE');
    const riskStats = this.risks.stats();
    const employees = this.employees.employees();

    let bodyExtra = '';
    if (options.reportType === 'DELAYED_TASKS' || options.reportType === 'TASKS_STATUS') {
      const rows = (options.reportType === 'DELAYED_TASKS' ? overdue : tasks)
        .slice(0, 100)
        .map(
          (tk) =>
            `<tr><td>${esc(tk.title)}</td><td>${esc(tk.status)}</td><td>${esc(tk.dueDate)}</td><td>${esc(tk.assignee ?? '')}</td></tr>`,
        )
        .join('');
      bodyExtra = `<h2>${t(options.reportType === 'DELAYED_TASKS' ? 'chart.delayedTasks' : 'chart.tasksStatus')}</h2>
        <table><thead><tr><th>${t('common.title')}</th><th>${t('common.status')}</th><th>${t('reports.dueDate')}</th><th>${t('reports.employee')}</th></tr></thead>
        <tbody>${rows || `<tr><td colspan="4">${t('reports.noData')}</td></tr>`}</tbody></table>`;
    } else if (options.reportType === 'EMPLOYEE_PERFORMANCE' || options.reportType === 'KPI_SUMMARY') {
      const rows = employees
        .slice(0, 100)
        .map(
          (e) =>
            `<tr><td>${esc(e.name)}</td><td>${esc(e.department || '')}</td><td>${esc(String(e.role || ''))}</td></tr>`,
        )
        .join('');
      bodyExtra = `<h2>${this.titleFor(options.reportType)}</h2>
        <table><thead><tr><th>${t('reports.employee')}</th><th>${t('common.department')}</th><th>${t('reports.role')}</th></tr></thead>
        <tbody>${rows || `<tr><td colspan="3">${t('reports.noData')}</td></tr>`}</tbody></table>`;
    }

    return `<!DOCTYPE html>
<html><head><meta charset="utf-8"><title>${esc(title)}</title>
<style>
  body { font-family: system-ui, sans-serif; padding: 32px; color: #1e293b; }
  h1 { color: #2563eb; margin-bottom: 4px; }
  h2 { font-size: 14px; margin: 24px 0 8px; border-bottom: 1px solid #e2e8f0; padding-bottom: 4px; }
  table { width: 100%; border-collapse: collapse; font-size: 12px; margin-top: 8px; }
  th, td { border: 1px solid #e2e8f0; padding: 8px; text-align: start; }
  th { background: #f8fafc; }
  .kpi { display: inline-block; margin-inline-end: 24px; }
  .kpi strong { font-size: 24px; display: block; color: #2563eb; }
  .meta { color: #64748b; font-size: 11px; }
  @media print { body { padding: 16px; } }
</style></head><body>
  <h1>STRATIX — ${esc(title)}</h1>
  <p class="meta">${t('reports.generatedAt')}: ${esc(generatedAt)}</p>
  <p class="meta">${t('reports.project')}: ${esc(projectName)} · ${t('reports.period')}: ${esc(options.dateFrom)} → ${esc(options.dateTo)}</p>
  <h2>${t('reports.executiveSummary')}</h2>
  <div>
    <span class="kpi"><strong>${projects.length}</strong>${t('kpi.totalProjects')}</span>
    <span class="kpi"><strong>${overdue.length}</strong>${t('dashboard.tasksOverdue')}</span>
    <span class="kpi"><strong>${riskStats.criticalRisks}</strong>${t('risks.statCritical')}</span>
    <span class="kpi"><strong>${schedule.filter((b) => b.status !== 'ON_TRACK').length}</strong>${t('dashboard.scheduleStatus')}</span>
  </div>
  <h2>${t('dashboard.projectHealth')}</h2>
  <table><thead><tr><th>${t('common.project')}</th><th>${t('reports.score')}</th><th>${t('common.status')}</th><th>${t('common.progress')}</th></tr></thead><tbody>${healthRows || `<tr><td colspan="4">${t('reports.noData')}</td></tr>`}</tbody></table>
  <h2>${t('dashboard.scheduleStatus')}</h2>
  <table><thead><tr><th>${t('common.project')}</th><th>${t('common.progress')} (${t('dashboard.plan')})</th><th>${t('common.progress')} (${t('dashboard.spent')})</th><th>${t('common.status')}</th></tr></thead><tbody>${budgetRows || `<tr><td colspan="4">${t('reports.noData')}</td></tr>`}</tbody></table>
  ${bodyExtra}
  <p class="meta" style="margin-top:32px">STRATIX © ${new Date().getFullYear()}</p>
</body></html>`;
  }

  private buildCsv(options: GenerateReportOptions): string {
    const ids = this.filteredProjectIds(options.projectId);
    const schedule = computeScheduleStatus(
      this.projects.projects().filter((p) => !ids || ids.has(p.id)),
    );
    const rows: (string | number)[][] = [
      ['Project', 'Health', 'Progress %', 'Expected %', 'Schedule Status', 'Period From', 'Period To'],
      ...this.health
        .all()
        .filter((p) => !ids || ids.has(p.projectId))
        .map((p) => {
          const b = schedule.find((x) => x.projectId === p.projectId);
          return [
            p.projectName,
            p.score,
            p.progress,
            b ? Math.round(b.budget / 1000) : '',
            b?.status ?? '',
            options.dateFrom,
            options.dateTo,
          ];
        }),
    ];

    if (options.reportType === 'DELAYED_TASKS' || options.reportType === 'TASKS_STATUS') {
      const tasks = this.tasksInPeriod(options.projectId, options.dateFrom, options.dateTo);
      const today = new Date().toISOString().slice(0, 10);
      const list =
        options.reportType === 'DELAYED_TASKS'
          ? tasks.filter((tk) => tk.dueDate < today && tk.status !== 'DONE')
          : tasks;
      rows.push([]);
      rows.push(['Task', 'Status', 'Due Date', 'Assignee']);
      for (const tk of list) {
        rows.push([tk.title, tk.status, tk.dueDate, tk.assignee ?? '']);
      }
    }

    return rows.map((r) => r.map((c) => `"${String(c).replace(/"/g, '""')}"`).join(',')).join('\n');
  }

  private async persist(file: File, meta: CreateReportPayload): Promise<ReportResponse | null> {
    return this.reportsStore.saveExport(meta, file);
  }
}

function esc(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}
