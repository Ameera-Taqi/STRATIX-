import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { ApiService } from '../../core/services/api.service';
import { RisksStore } from '../../core/services/risks.store';
import { ProjectsStore } from '../../core/services/projects.store';
import { TasksStore } from '../../core/services/tasks.store';
import { EmployeesStore } from '../../core/services/employees.store';
import { DepartmentsStore } from '../../core/services/departments.store';
import { DashboardInsightsService } from '../../core/services/dashboard-insights.service';
import { NotificationsStore } from '../../core/services/notifications.store';
import { CurrentUserService } from '../../core/services/current-user.service';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { LanguageService } from '../../core/i18n/language.service';
import { ProjectHealthService } from '../../core/services/project-health.service';
import { HEALTH_COLORS, healthStatusClass } from '../../shared/utils/project-health.util';
import { riskLevelClass } from '../../shared/utils/risk.util';
import { RoleAccessService } from '../../core/services/role-access.service';
import { DonutChartComponent } from '../../shared/components/charts/donut-chart.component';
import { BarChartComponent } from '../../shared/components/charts/bar-chart.component';
import { HorizontalBarChartComponent } from '../../shared/components/charts/horizontal-bar-chart.component';
import { ChartSegment } from '../../shared/components/charts/chart.types';
import { UiIconComponent } from '../../shared/components/ui-icon/ui-icon.component';
import {
  CHART_COLORS,
  CHART_LOAD,
  chartSeriesColor,
  chartTrackColor,
} from '../../shared/components/charts/chart-palette';
import { ThemeService } from '../../core/theme/theme.service';
import {
  DashboardWidget,
  dashboardHintKey,
  dashboardTitleKey,
  dashboardWidgetsFor,
} from '../../core/config/dashboard-widgets';
import { PlanRow } from '../../core/models/organization.model';
import { scoreBandKey } from '../../core/models/kpi-evaluation.model';

const LOAD_COLORS = CHART_LOAD;

interface SubscriptionUsage {
  planCode: string;
  status: string;
  usersUsed: number;
  usersMax: number;
  projectsUsed: number;
  projectsMax: number;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    TopbarComponent,
    RouterLink,
    TranslatePipe,
    DatePipe,
    DonutChartComponent,
    BarChartComponent,
    HorizontalBarChartComponent,
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
  private readonly projectsStore = inject(ProjectsStore);
  private readonly tasksStore = inject(TasksStore);
  private readonly employeesStore = inject(EmployeesStore);
  private readonly departmentsStore = inject(DepartmentsStore);
  private readonly insights = inject(DashboardInsightsService);
  private readonly notifications = inject(NotificationsStore);
  private readonly currentUser = inject(CurrentUserService);
  private readonly lang = inject(LanguageService);
  private readonly projectHealthService = inject(ProjectHealthService);
  private readonly theme = inject(ThemeService);
  private readonly roleAccess = inject(RoleAccessService);
  private readonly router = inject(Router);

  readonly apiConnected = signal(false);
  readonly subscriptionUsage = signal<SubscriptionUsage | null>(null);
  readonly myKpiScore = signal<number | null>(null);
  readonly myKpiPeriod = signal<string | null>(null);

  readonly role = computed(() => this.roleAccess.role());
  readonly titleKey = computed(() => dashboardTitleKey(this.role()));
  readonly hintKey = computed(() => dashboardHintKey(this.role()));
  readonly widgets = computed(() => new Set(dashboardWidgetsFor(this.role())));

  readonly projectHealth = this.projectHealthService.all;
  readonly overdueTasks = this.insights.overdueTasks;
  readonly employeeLoad = this.insights.employeeLoad;
  readonly departmentPerformance = this.insights.departmentPerformance;
  readonly riskStats = this.risksStore.stats;
  readonly overallHealth = this.projectHealthService.portfolioScore;
  readonly notificationItems = this.notifications.items;

  readonly isFreshWorkspace = computed(
    () => this.projectsStore.loaded() && this.projectsStore.projects().length === 0,
  );

  readonly showAdminEmpty = computed(() => {
    const role = this.role();
    return (
      this.isFreshWorkspace() &&
      (role === 'ORG_ADMIN' || role === 'ADMIN' || role === 'SUPER_ADMIN' || role === 'PROJECT_MANAGER')
    );
  });

  readonly setupSteps = computed(() => {
    const hasDepartment = this.departmentsStore.departments().length > 0;
    const hasTeam = this.employeesStore.employees().length > 1;
    const hasProject = this.projectsStore.projects().length > 0;
    return [
      { key: 'org', done: true, labelKey: 'dashboard.setupOrg' as const },
      { key: 'dept', done: hasDepartment, labelKey: 'dashboard.setupDept' as const, link: '/departments' },
      { key: 'team', done: hasTeam, labelKey: 'dashboard.setupTeam' as const, link: '/team' },
      { key: 'project', done: hasProject, labelKey: 'dashboard.setupProject' as const, link: '/projects' },
    ];
  });

  readonly canCreateProject = computed(() => this.roleAccess.canWrite('PROJECTS'));

  readonly meId = computed(() => this.currentUser.profile()?.id ?? null);

  readonly myProjects = computed(() => {
    const id = this.meId();
    const all = this.projectsStore.projects();
    if (id == null) return all;
    const mine = all.filter((p) => p.managerId === id);
    return mine.length > 0 ? mine : all;
  });

  readonly myProjectIds = computed(() => new Set(this.myProjects().map((p) => p.id)));

  readonly scopedTasks = computed(() => {
    const role = this.role();
    const tasks = this.tasksStore.getAll();
    const me = this.meId();
    if (role === 'EMPLOYEE' && me != null) {
      return tasks.filter((t) => t.assigneeId === me);
    }
    if (role === 'PROJECT_MANAGER' || role === 'TEAM_LEADER') {
      const ids = this.myProjectIds();
      return tasks.filter((t) => ids.has(t.projectId));
    }
    return tasks;
  });

  readonly today = computed(() => new Date().toISOString().slice(0, 10));

  readonly teamOpenTasks = computed(() =>
    this.scopedTasks()
      .filter((t) => t.status !== 'DONE')
      .slice(0, 8),
  );

  /** Open tasks for the current persona (employee dashboard widget). */
  readonly myTasks = computed(() =>
    this.scopedTasks()
      .filter((t) => t.status !== 'DONE')
      .slice(0, 8),
  );

  readonly dueToday = computed(() => {
    const today = this.today();
    return this.scopedTasks().filter((t) => t.status !== 'DONE' && t.dueDate === today);
  });

  readonly myOverdue = computed(() => {
    const today = this.today();
    return this.scopedTasks().filter((t) => t.status !== 'DONE' && t.dueDate < today);
  });

  readonly inReview = computed(() => this.scopedTasks().filter((t) => t.status === 'REVIEW'));

  readonly blockedTasks = computed(() => this.scopedTasks().filter((t) => t.status === 'BLOCKED'));

  readonly delayedTasks = computed(() => {
    const today = this.today();
    return this.scopedTasks()
      .filter((t) => t.status !== 'DONE' && t.dueDate < today)
      .slice(0, 8);
  });

  readonly criticalRisks = computed(() =>
    this.risksStore
      .risks()
      .filter((r) => r.status !== 'CLOSED' && (r.riskLevel === 'CRITICAL' || r.riskLevel === 'HIGH'))
      .sort((a, b) => {
        const order = { CRITICAL: 0, HIGH: 1, MEDIUM: 2, LOW: 3 } as const;
        return order[a.riskLevel] - order[b.riskLevel];
      })
      .slice(0, 6),
  );

  readonly criticalProjects = computed(() =>
    this.projectHealth().filter((p) => p.status === 'CRITICAL' || p.status === 'WARNING').slice(0, 6),
  );

  readonly recentProjects = computed(() => this.projectsStore.projects().slice(0, 6));

  readonly featureProgress = computed(() =>
    this.myProjects().map((p, i) => ({
      label: p.name.split(' ')[0],
      value: p.progress,
      color: chartSeriesColor(i),
      sublabel: `${p.progress}%`,
    })),
  );

  readonly projectHealthBars = computed(() =>
    this.projectHealth()
      .filter((p) => {
        const role = this.role();
        if (role === 'PROJECT_MANAGER') return this.myProjectIds().has(p.projectId);
        return true;
      })
      .map((p, i) => ({
        label: p.projectName.split(' ')[0],
        value: p.score,
        color: chartSeriesColor(i),
        sublabel: `${p.progress}%`,
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

  readonly portfolioDonut = computed((): ChartSegment[] => {
    this.lang.lang();
    const h = this.overallHealth();
    return [
      { label: this.lang.t('dashboard.health.' + h.status), value: h.score, color: HEALTH_COLORS[h.status] },
      {
        label: this.lang.t('dashboard.chartRemaining'),
        value: 100 - h.score,
        color: chartTrackColor(this.theme.isDark()),
      },
    ];
  });

  readonly healthMixDonut = computed((): ChartSegment[] => {
    this.lang.lang();
    const counts: Record<'HEALTHY' | 'WARNING' | 'CRITICAL', number> = {
      HEALTHY: 0,
      WARNING: 0,
      CRITICAL: 0,
    };
    for (const p of this.projectHealth()) counts[p.status]++;
    return (['HEALTHY', 'WARNING', 'CRITICAL'] as const)
      .filter((s) => counts[s] > 0)
      .map((s) => ({
        label: this.lang.t('dashboard.health.' + s),
        value: counts[s],
        color: s === 'HEALTHY' ? CHART_COLORS.teal : s === 'WARNING' ? CHART_COLORS.purple : CHART_COLORS.blue,
      }));
  });

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

  readonly kpiBandKey = scoreBandKey;
  readonly healthStatusClass = healthStatusClass;
  readonly riskLevelClass = riskLevelClass;

  has(widget: DashboardWidget): boolean {
    return this.widgets().has(widget);
  }

  createFirstProject(): void {
    void this.router.navigate(['/projects'], { queryParams: { create: '1' } });
  }

  usagePct(used: number, max: number): number {
    if (max <= 0) return 0;
    return Math.min(100, Math.round((used / max) * 100));
  }

  ngOnInit(): void {
    this.api.health().subscribe({ next: () => this.apiConnected.set(true) });
    this.risksStore.loadFromApi();
    this.projectsStore.loadFromApi(() => this.tasksStore.loadFromApi());
    this.employeesStore.loadFromApi();
    void this.departmentsStore.load();
    this.notifications.loadFromApi();
    this.loadSubscriptionUsage();
    this.loadMyKpi();
  }

  private loadSubscriptionUsage(): void {
    if (!this.has('subscriptionUsage')) return;
    forkJoin({
      sub: this.api.getCurrentSubscription().pipe(catchError(() => of(null))),
      plans: this.api.getPlans().pipe(catchError(() => of([] as PlanRow[]))),
    }).subscribe(({ sub, plans }) => {
      if (!sub) return;
      const plan = this.matchPlan(plans, sub.planCode);
      this.subscriptionUsage.set({
        planCode: sub.planCode,
        status: sub.status,
        usersUsed: this.employeesStore.employees().length,
        usersMax: plan?.maxUsers ?? 0,
        projectsUsed: this.projectsStore.projects().length,
        projectsMax: plan?.maxProjects ?? 0,
      });
    });
  }

  private matchPlan(plans: PlanRow[], planCode: string): PlanRow | undefined {
    const code = (planCode || '').toUpperCase();
    const byName = plans.find((p) => p.name.toUpperCase() === code);
    if (byName) return byName;
    // Plan catalog uses Free / Pro / Enterprise names
    const map: Record<string, string> = {
      FREE: 'FREE',
      PRO: 'PRO',
      ENTERPRISE: 'ENTERPRISE',
      TRIAL: 'FREE',
    };
    const target = map[code] ?? code;
    return plans.find((p) => p.name.toUpperCase().includes(target));
  }

  private loadMyKpi(): void {
    if (!this.has('myKpi')) return;
    this.api
      .getKpiEvaluations()
      .pipe(catchError(() => of([])))
      .subscribe((list) => {
        const me = this.meId();
        const mine = list
          .filter((e) => (me == null || e.userId === me) && e.overallScore != null)
          .sort((a, b) => (b.approvedAt ?? b.submittedAt ?? '').localeCompare(a.approvedAt ?? a.submittedAt ?? ''));
        const latest = mine[0];
        if (latest?.overallScore != null) {
          this.myKpiScore.set(latest.overallScore);
          this.myKpiPeriod.set(latest.periodName);
        }
      });
  }
}
