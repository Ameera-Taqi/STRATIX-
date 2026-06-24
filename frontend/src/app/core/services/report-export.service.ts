import { Injectable, inject } from '@angular/core';
import { LanguageService } from '../i18n/language.service';
import { AuditLogStore } from './audit-log.store';
import { ProjectsStore } from './projects.store';
import { TasksStore } from './tasks.store';
import { RisksStore } from './risks.store';
import { MOCK_BUDGET_STATUS } from '../data/dashboard-insights';
import { ProjectHealthService } from './project-health.service';

@Injectable({ providedIn: 'root' })
export class ReportExportService {
  private readonly lang = inject(LanguageService);
  private readonly audit = inject(AuditLogStore);
  private readonly projects = inject(ProjectsStore);
  private readonly tasks = inject(TasksStore);
  private readonly risks = inject(RisksStore);
  private readonly health = inject(ProjectHealthService);

  exportExecutivePdf(): void {
    const t = (k: string) => this.lang.t(k);
    const generatedAt = new Date().toLocaleString();
    const projectRows = this.health.all().map(
      (p) => `<tr><td>${p.projectName}</td><td>${p.score}%</td><td>${p.status}</td><td>${p.progress}%</td></tr>`,
    ).join('');
    const budgetRows = MOCK_BUDGET_STATUS.map(
      (b) => `<tr><td>${b.projectName}</td><td>$${(b.budget / 1000).toFixed(0)}k</td><td>$${(b.spent / 1000).toFixed(0)}k</td><td>${b.status}</td></tr>`,
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
    <span class="kpi"><strong>${MOCK_BUDGET_STATUS.filter((b) => b.status !== 'ON_TRACK').length}</strong>${t('dashboard.budgetAlerts')}</span>
  </div>
  <h2>${t('dashboard.projectHealth')}</h2>
  <table><thead><tr><th>${t('common.project')}</th><th>${t('reports.score')}</th><th>${t('common.status')}</th><th>${t('common.progress')}</th></tr></thead><tbody>${projectRows}</tbody></table>
  <h2>${t('dashboard.budgetStatus')}</h2>
  <table><thead><tr><th>${t('common.project')}</th><th>${t('dashboard.budget')}</th><th>${t('dashboard.spent')}</th><th>${t('common.status')}</th></tr></thead><tbody>${budgetRows}</tbody></table>
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

    this.audit.log({
      entityType: 'REPORT',
      entityId: 0,
      entityLabel: t('reports.executiveTitle'),
      action: 'EXPORT',
      details: 'Executive PDF generated',
    });
  }
}
