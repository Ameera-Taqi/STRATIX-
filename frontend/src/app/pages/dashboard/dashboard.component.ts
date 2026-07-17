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
import { UiIconComponent } from '../../shared/components/ui-icon/ui-icon.component';
import {
  CHART_COLORS,
  CHART_LOAD,
  CHART_PRIORITY,
  CHART_RISK,
  chartSeriesColor,
  chartTrackColor,
} from '../../shared/components/charts/chart-palette';
import { ThemeService } from '../../core/theme/theme.service';

const LOAD_COLORS = CHART_LOAD;
const PRIORITY_COLORS: Record<string, string> = { ...CHART_PRIORITY };



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

    UiIconComponent,

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
  private readonly theme = inject(ThemeService);

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

      { label: this.lang.t('dashboard.chartRemaining'), value: 100 - h.score, color: chartTrackColor(this.theme.isDark()) },

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

        color: s === 'HEALTHY' ? CHART_COLORS.teal : s === 'WARNING' ? CHART_COLORS.purple : CHART_COLORS.blue,

      }));

  });



  readonly projectHealthBars = computed(() =>

    this.projectHealth().map((p, i) => ({

      label: p.projectName.split(' ')[0],

      value: p.score,

      color: chartSeriesColor(i),

      sublabel: `${p.progress}%`,

    })),

  );



  readonly overdueTaskBars = computed(() =>
    this.overdueTasks().map((t, i) => ({

      label: t.title.split(' ').slice(0, 2).join(' '),

      value: t.daysOverdue,

      color: chartSeriesColor(i),

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
    this.departmentPerformance().map((d, i) => ({

      label: d.department,

      value: d.score,

      color: chartSeriesColor(i),

      sublabel: `${d.trend >= 0 ? '+' : ''}${d.trend}%`,

    })),

  );



  readonly riskDonut = computed((): ChartSegment[] => {

    this.lang.lang();

    const s = this.riskStats();

    const segments = [

      { label: this.lang.t('risks.statOpen'), value: s.openRisks, color: CHART_COLORS.purple },

      { label: this.lang.t('risks.statCritical'), value: s.criticalRisks, color: CHART_COLORS.coral },

      { label: this.lang.t('risks.statClosed'), value: s.closedRisks, color: CHART_COLORS.teal },

    ].filter((seg) => seg.value > 0);

    return segments.length > 0
      ? segments
      : [{ label: this.lang.t('risks.statTotal'), value: 1, color: chartTrackColor(this.theme.isDark()) }];

  });



  readonly kpiSparkBars = computed(() => {

    this.lang.lang();

    return [

      { label: this.lang.t('dashboard.health.CRITICAL'), value: this.projectHealth().filter((p) => p.status === 'CRITICAL').length, color: CHART_COLORS.blue },

      { label: this.lang.t('dashboard.tasksOverdue'), value: this.overdueTasks().length, color: CHART_COLORS.purple },

      { label: this.lang.t('dashboard.riskLevel'), value: this.riskStats().criticalRisks, color: CHART_COLORS.teal },

      { label: this.lang.t('dashboard.budgetAlerts'), value: this.budgetAlertsCount(), color: CHART_COLORS.amber },

    ];

  });



  readonly budgetSeries = computed(() => {
    this.lang.lang();
    return [
      { key: 'budget', label: this.lang.t('dashboard.plan'), color: CHART_COLORS.blue },
      { key: 'spent', label: this.lang.t('common.progress'), color: CHART_COLORS.purple },
      { key: 'forecast', label: this.lang.t('dashboard.forecast'), color: CHART_COLORS.teal },
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

