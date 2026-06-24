import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { ApiService } from '../../core/services/api.service';
import { RisksStore } from '../../core/services/risks.store';
import { DashboardInsightsService } from '../../core/services/dashboard-insights.service';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { LanguageService } from '../../core/i18n/language.service';
import { budgetStatusClass, loadStatusClass } from '../../core/data/dashboard-insights';
import { ProjectHealthService } from '../../core/services/project-health.service';
import { HEALTH_COLORS, healthStatusClass } from '../../shared/utils/project-health.util';
import { riskLevelClass } from '../../shared/utils/risk.util';

import { DonutChartComponent } from '../../shared/components/charts/donut-chart.component';

import { BarChartComponent } from '../../shared/components/charts/bar-chart.component';

import { HorizontalBarChartComponent } from '../../shared/components/charts/horizontal-bar-chart.component';

import { GroupedBarChartComponent } from '../../shared/components/charts/grouped-bar-chart.component';

import { ChartSegment } from '../../shared/components/charts/chart.types';

const LOAD_COLORS = { NORMAL: '#10b981', HIGH: '#f59e0b', OVERLOADED: '#ef4444' };

const PRIORITY_COLORS: Record<string, string> = {

  URGENT: '#ef4444',

  HIGH: '#f97316',

  MEDIUM: '#f59e0b',

  LOW: '#3b82f6',

};



@Component({

  selector: 'app-dashboard',

  standalone: true,

  imports: [

    TopbarComponent,

    RouterLink,

    TranslatePipe,

    DonutChartComponent,

    BarChartComponent,

    HorizontalBarChartComponent,

    GroupedBarChartComponent,

  ],

  templateUrl: './dashboard.component.html',

  styles: `

    :host {

      display: flex;

      flex: 1 1 auto;

      flex-direction: column;

      min-height: 0;

      overflow: hidden;

    }

  `,

})

export class DashboardComponent implements OnInit {

  private readonly api = inject(ApiService);
  private readonly risksStore = inject(RisksStore);
  private readonly insights = inject(DashboardInsightsService);
  private readonly lang = inject(LanguageService);
  private readonly projectHealthService = inject(ProjectHealthService);

  readonly apiConnected = signal(false);
  readonly projectHealth = this.projectHealthService.all;
  readonly overdueTasks = this.insights.overdueTasks;
  readonly employeeLoad = this.insights.employeeLoad;
  readonly departmentPerformance = this.insights.departmentPerformance;
  readonly budgetStatus = this.insights.scheduleStatus;



  readonly riskStats = this.risksStore.stats;

  readonly topRisks = computed(() =>

    this.risksStore

      .risks()

      .filter((r) => r.status !== 'CLOSED')

      .sort((a, b) => {

        const order = { CRITICAL: 0, HIGH: 1, MEDIUM: 2, LOW: 3 };

        return order[a.riskLevel] - order[b.riskLevel];

      })

      .slice(0, 4),

  );



  readonly overallHealth = this.projectHealthService.portfolioScore;



  readonly budgetAlertsCount = computed(
    () => this.budgetStatus().filter((b) => b.status !== 'ON_TRACK').length,
  );

  readonly kpiSparkMax = computed(() => {
    const values = [
      this.projectHealth().filter((p) => p.status === 'CRITICAL').length,
      this.overdueTasks().length,
      this.riskStats().criticalRisks,
      this.budgetAlertsCount(),
    ];
    return Math.max(5, ...values, 1);
  });



  readonly portfolioDonut = computed((): ChartSegment[] => {

    this.lang.lang();

    const h = this.overallHealth();

    return [

      { label: this.lang.t('dashboard.health.' + h.status), value: h.score, color: HEALTH_COLORS[h.status] },

      { label: this.lang.t('dashboard.chartRemaining'), value: 100 - h.score, color: '#e2e8f0' },

    ];

  });



  readonly healthMixDonut = computed((): ChartSegment[] => {

    this.lang.lang();

    const counts: Record<'HEALTHY' | 'WARNING' | 'CRITICAL', number> = { HEALTHY: 0, WARNING: 0, CRITICAL: 0 };

    for (const p of this.projectHealth()) counts[p.status]++;
    return (['HEALTHY', 'WARNING', 'CRITICAL'] as const)

      .filter((s) => counts[s] > 0)

      .map((s) => ({

        label: this.lang.t('dashboard.health.' + s),

        value: counts[s],

        color: HEALTH_COLORS[s],

      }));

  });



  readonly projectHealthBars = computed(() =>

    this.projectHealth().map((p) => ({

      label: p.projectName.split(' ')[0],

      value: p.score,

      color: HEALTH_COLORS[p.status],

      sublabel: `${p.progress}%`,

    })),

  );



  readonly overdueTaskBars = computed(() =>
    this.overdueTasks().map((t) => ({

      label: t.title.split(' ').slice(0, 2).join(' '),

      value: t.daysOverdue,

      color: PRIORITY_COLORS[t.priority] ?? '#3b82f6',

      sublabel: t.priority,

    })),

  );



  readonly employeeLoadBars = computed(() =>
    this.employeeLoad().map((e) => ({

      label: e.name.split(' ')[0],

      value: e.loadPct,

      color: LOAD_COLORS[e.status],

      sublabel: `${e.activeTasks}/${e.capacity}`,

    })),

  );



  readonly departmentBars = computed(() =>
    this.departmentPerformance().map((d) => ({

      label: d.department,

      value: d.score,

      color: '#3b82f6',

      sublabel: `${d.trend >= 0 ? '+' : ''}${d.trend}%`,

    })),

  );



  readonly riskDonut = computed((): ChartSegment[] => {

    this.lang.lang();

    const s = this.riskStats();

    const segments = [

      { label: this.lang.t('risks.statOpen'), value: s.openRisks, color: '#ef4444' },

      { label: this.lang.t('risks.statCritical'), value: s.criticalRisks, color: '#f97316' },

      { label: this.lang.t('risks.statClosed'), value: s.closedRisks, color: '#10b981' },

    ].filter((seg) => seg.value > 0);

    return segments.length > 0
      ? segments
      : [{ label: this.lang.t('risks.statTotal'), value: 1, color: '#e2e8f0' }];

  });



  readonly kpiSparkBars = computed(() => {

    this.lang.lang();

    return [

      { label: this.lang.t('dashboard.health.CRITICAL'), value: this.projectHealth().filter((p) => p.status === 'CRITICAL').length, color: '#ef4444' },

      { label: this.lang.t('dashboard.tasksOverdue'), value: this.overdueTasks().length, color: '#f97316' },

      { label: this.lang.t('dashboard.riskLevel'), value: this.riskStats().criticalRisks, color: '#f59e0b' },

      { label: this.lang.t('dashboard.budgetAlerts'), value: this.budgetAlertsCount(), color: '#8b5cf6' },

    ];

  });



  readonly budgetSeries = computed(() => {
    this.lang.lang();
    return [
      { key: 'budget', label: this.lang.t('dashboard.plan'), color: '#94a3b8' },
      { key: 'spent', label: this.lang.t('common.progress'), color: '#3b82f6' },
      { key: 'forecast', label: this.lang.t('dashboard.forecast'), color: '#f59e0b' },
    ];
  });



  readonly budgetGrouped = computed(() =>
    this.budgetStatus().map((b) => ({

      label: b.projectName.split(' ')[0],

      values: {

        budget: Math.round(b.budget / 1000),

        spent: Math.round(b.spent / 1000),

        forecast: Math.round(b.forecast / 1000),

      },

    })),

  );



  readonly healthStatusClass = healthStatusClass;

  readonly loadStatusClass = loadStatusClass;

  readonly budgetStatusClass = budgetStatusClass;

  readonly riskLevelClass = riskLevelClass;

  budgetUsedPct(spent: number, budget: number): number {

    return Math.min(Math.round((spent / budget) * 100), 100);

  }



  ngOnInit(): void {

    this.api.health().subscribe({ next: () => this.apiConnected.set(true) });

    this.risksStore.loadFromApi();
  }

}

