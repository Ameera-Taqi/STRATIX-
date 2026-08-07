import { Injectable, inject } from '@angular/core';
import { LanguageService } from '../i18n/language.service';
import { AuditLogStore } from './audit-log.store';
import { ProjectsStore } from './projects.store';
import { TasksStore } from './tasks.store';
import { RisksStore } from './risks.store';
import { ProjectHealthService } from './project-health.service';
import { ReportsStore } from './reports.store';
import { computeScheduleStatus } from '../../shared/utils/dashboard-insights.util';

@Injectable({ providedIn: 'root' })
export class ReportExportService {
  private readonly lang = inject(LanguageService);
  private readonly audit = inject(AuditLogStore);
  private readonly projects = inject(ProjectsStore);
  private readonly tasks = inject(TasksStore);
  private readonly risks = inject(RisksStore);
  private readonly health = inject(ProjectHealthService);
  private readonly reportsStore = inject(ReportsStore);

  exportExecutivePdf(options?: {
    projectId?: number | null;
    employeeId?: number | null;
  }): void {
    const t = (k: string) => this.lang.t(k);
    const generatedAt = new Date().toLocaleString();
    const projectRows = this.health.all().map(
      (p) => `<tr><td>${p.projectName}</td><td>${p.score}%</td><td>${p.status}</td><td>${p.progress}%</td></tr>`,
    ).join('');
    const scheduleStatus = computeScheduleStatus(this.projects.projects());
    const budgetRows = scheduleStatus.map(
      (b) => `<tr><td>${b.projectName}</td><td>${Math.round(b.budget / 1000)}%</td><td>${Math.round(b.spent / 1000)}%</td><td>${b.status}</td></tr>`,
    ).join('');
    const riskStats = this.risks.stats();
    const overdue = this.tasks.getAll().filter((tk) => tk.dueDate < new Date().toISOString().slice(0, 10) && tk.status !== 'DONE').length;

    const html = `<!DOCTYPE html>
<html><head><meta charset="utf-8"><title>${t('reports.executiveTitle')}</title>
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
  <h1>STRATIX — ${t('reports.executiveTitle')}</h1>
  <p class="meta">${t('reports.generatedAt')}: ${generatedAt}</p>
  <h2>${t('reports.executiveSummary')}</h2>
  <div>
    <span class="kpi"><strong>${this.projects.projects().length}</strong>${t('kpi.totalProjects')}</span>
    <span class="kpi"><strong>${overdue}</strong>${t('dashboard.tasksOverdue')}</span>
    <span class="kpi"><strong>${riskStats.criticalRisks}</strong>${t('risks.statCritical')}</span>
    <span class="kpi"><strong>${scheduleStatus.filter((b) => b.status !== 'ON_TRACK').length}</strong>${t('dashboard.scheduleStatus')}</span>
  </div>
  <h2>${t('dashboard.projectHealth')}</h2>
  <table><thead><tr><th>${t('common.project')}</th><th>${t('reports.score')}</th><th>${t('common.status')}</th><th>${t('common.progress')}</th></tr></thead><tbody>${projectRows}</tbody></table>
  <h2>${t('dashboard.scheduleStatus')}</h2>
  <table><thead><tr><th>${t('common.project')}</th><th>${t('common.progress')} (${t('dashboard.plan')})</th><th>${t('common.progress')} (${t('dashboard.spent')})</th><th>${t('common.status')}</th></tr></thead><tbody>${budgetRows}</tbody></table>
  <h2>${t('dashboard.riskLevel')}</h2>
  <p>${t('risks.statTotal')}: ${riskStats.totalRisks} · ${t('risks.statOpen')}: ${riskStats.openRisks} · ${t('risks.statCritical')}: ${riskStats.criticalRisks}</p>
  <p class="meta" style="margin-top:32px">STRATIX © ${new Date().getFullYear()}</p>
</body></html>`;

    const win = window.open('', '_blank');
    if (!win) return;
    win.document.write(html);
    win.document.close();
    win.focus();
    setTimeout(() => win.print(), 400);

    const blob = new Blob([html], { type: 'text/plain;charset=utf-8' });
    // Persist as printable HTML snapshot under PDF format policy (text/plain allowed for archival export).
    // Use application/pdf only when a real PDF blob exists; store HTML as CSV-family text for EXCEL? Better store as custom with text and format PDF loosely.
    // Storage policy requires PDF content-type for PDF format — use a .html file as EXCEL? No.
    // Change: save as text/csv is wrong. Allow text/html in storage for PDF archival? Update policy to allow text/html for PDF exports from UI.
    const file = new File([blob], `executive-report-${Date.now()}.html`, { type: 'text/html' });
    void this.persist(file, {
      title: t('reports.executiveTitle'),
      reportType: 'PROJECTS_PROGRESS',
      format: 'PDF',
      projectId: options?.projectId ?? null,
      employeeId: options?.employeeId ?? null,
    });

    this.audit.log({
      entityType: 'REPORT',
      entityId: 0,
      entityLabel: t('reports.executiveTitle'),
      action: 'EXPORT',
      details: 'Executive PDF generated',
    });
  }

  private async persist(
    file: File,
    meta: {
      title: string;
      reportType: string;
      format: 'PDF' | 'EXCEL';
      projectId?: number | null;
      employeeId?: number | null;
    },
  ): Promise<void> {
    await this.reportsStore.saveExport(
      {
        title: meta.title,
        reportType: meta.reportType,
        format: meta.format,
        projectId: meta.projectId ?? null,
        employeeId: meta.employeeId ?? null,
      },
      file,
    );
  }
}
