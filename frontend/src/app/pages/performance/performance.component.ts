import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError, finalize, switchMap } from 'rxjs/operators';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { LanguageService } from '../../core/i18n/language.service';
import { ApiService } from '../../core/services/api.service';
import { CurrentUserService } from '../../core/services/current-user.service';
import { EmployeesStore } from '../../core/services/employees.store';
import { RoleAccessService } from '../../core/services/role-access.service';
import {
  EmployeeEvaluation,
  EmployeeKpiResult,
  EvaluationPeriod,
  buildKpiBreakdown,
  evaluationStatusLabelKey,
  formatKpiNumber,
  isEmployeeVisibleStatus,
  isManagerKpi,
  isSystemKpi,
  scoreBandKey,
} from '../../core/models/kpi-evaluation.model';
import { UiIconComponent } from '../../shared/components/ui-icon/ui-icon.component';
import { WarnUnsavedDirective } from '../../shared/directives/warn-unsaved.directive';
import {
  allowLeaveIfClean,
  formSnapshot,
  HasUnsavedChanges,
  isFormDirty,
} from '../../core/unsaved/unsaved-changes';

interface EmployeeEvalRow {
  userId: number;
  name: string;
  evaluation: EmployeeEvaluation | null;
}

@Component({
  selector: 'app-performance',
  standalone: true,
  imports: [TopbarComponent, TranslatePipe, FormsModule, UiIconComponent, WarnUnsavedDirective],
  templateUrl: './performance.component.html',
})
export class PerformanceComponent implements OnInit, HasUnsavedChanges {
  private readonly api = inject(ApiService);
  private readonly employeesStore = inject(EmployeesStore);
  private readonly roleAccess = inject(RoleAccessService);
  private readonly currentUser = inject(CurrentUserService);
  private readonly lang = inject(LanguageService);
  private draftBaseline: string | null = null;

  readonly loading = signal(false);
  readonly detailLoading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);

  readonly periods = signal<EvaluationPeriod[]>([]);
  readonly selectedPeriodId = signal<number | null>(null);
  readonly evaluations = signal<EmployeeEvaluation[]>([]);
  readonly activeEval = signal<EmployeeEvaluation | null>(null);
  /** Local editable manager scores keyed by result id. */
  readonly managerScores = signal<Record<number, number>>({});
  readonly managerNotes = signal('');

  readonly canManage = computed(() => {
    const role = this.roleAccess.role();
    return (
      role === 'SUPER_ADMIN' ||
      role === 'ORG_ADMIN' ||
      role === 'ADMIN' ||
      role === 'PROJECT_MANAGER' ||
      role === 'TEAM_LEADER'
    );
  });

  readonly canApprove = computed(() => {
    const role = this.roleAccess.role();
    return role === 'SUPER_ADMIN' || role === 'ORG_ADMIN' || role === 'ADMIN';
  });

  readonly selectedPeriod = computed(() => {
    const id = this.selectedPeriodId();
    return this.periods().find((p) => p.id === id) ?? null;
  });

  readonly employeeRows = computed((): EmployeeEvalRow[] => {
    const evals = this.evaluations();
    const byUser = new Map(evals.map((e) => [e.userId, e]));
    return this.employeesStore
      .employees()
      .filter((e) => e.status === 'Active')
      .map((e) => ({
        userId: e.id,
        name: e.name,
        evaluation: byUser.get(e.id) ?? null,
      }))
      .sort((a, b) => a.name.localeCompare(b.name));
  });

  readonly systemResults = computed(() => {
    const ev = this.activeEval();
    return (ev?.results ?? []).filter(isSystemKpi);
  });

  readonly managerResults = computed(() => {
    const ev = this.activeEval();
    return (ev?.results ?? []).filter(isManagerKpi);
  });

  /** DRAFT with no results yet — primary action is Calculate. */
  readonly needsCalculate = computed(() => {
    const ev = this.activeEval();
    return !!ev && ev.status === 'DRAFT' && (ev.results?.length ?? 0) === 0;
  });

  /** DRAFT after metrics exist — treated as CALCULATED for action UX. */
  readonly isCalculatedDraft = computed(() => {
    const ev = this.activeEval();
    return !!ev && ev.status === 'DRAFT' && (ev.results?.length ?? 0) > 0;
  });

  /**
   * Workflow stage for action buttons (never a status dropdown).
   * CALCULATED is a UX stage while API status remains DRAFT with results.
   */
  readonly workflowStage = computed(():
    | 'DRAFT'
    | 'CALCULATED'
    | 'SUBMITTED'
    | 'IN_REVIEW'
    | 'APPROVED'
    | 'REJECTED'
    | null => {
    const ev = this.activeEval();
    if (!ev) return null;
    if (ev.status === 'DRAFT' && (ev.results?.length ?? 0) === 0) return 'DRAFT';
    if (ev.status === 'DRAFT') return 'CALCULATED';
    if (ev.status === 'SUBMITTED') return 'SUBMITTED';
    if (ev.status === 'IN_REVIEW') return 'IN_REVIEW';
    if (ev.status === 'APPROVED') return 'APPROVED';
    if (ev.status === 'REJECTED') return 'REJECTED';
    return null;
  });

  readonly workflowStageLabelKey = computed(() => {
    const stage = this.workflowStage();
    switch (stage) {
      case 'DRAFT':
        return 'eval.status.draft';
      case 'CALCULATED':
        return 'eval.status.calculated';
      case 'SUBMITTED':
        return 'eval.status.submitted';
      case 'IN_REVIEW':
        return 'eval.status.underReview';
      case 'APPROVED':
        return 'eval.status.approved';
      case 'REJECTED':
        return 'eval.status.rejected';
      default:
        return 'eval.status.notStarted';
    }
  });

  /** Transparent weighted breakdown — what the employee should always see. */
  readonly breakdownRows = computed(() => {
    const ev = this.activeEval();
    if (!ev?.results?.length) return [];
    return buildKpiBreakdown(ev.results, this.managerScores());
  });

  /** Live overall while manager edits draft scores. */
  readonly liveOverall = computed(() => {
    const rows = this.breakdownRows();
    if (rows.length === 0) return this.activeEval()?.overallScore ?? 0;
    return rows.reduce((s, r) => s + r.contribution, 0);
  });

  readonly statusKey = evaluationStatusLabelKey;
  readonly bandKey = scoreBandKey;
  readonly fmt = formatKpiNumber;

  rowStatusKey(row: EmployeeEvalRow): string {
    const ev = row.evaluation;
    if (!ev) return 'eval.status.notStarted';
    if (ev.status === 'DRAFT' && (ev.results?.length ?? 0) > 0) return 'eval.status.calculated';
    return evaluationStatusLabelKey(ev.status);
  }

  ngOnInit(): void {
    if (this.canManage()) this.employeesStore.loadAdministrationFromApi();
    this.loadPeriods();
  }

  loadPeriods(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.getKpiPeriods().subscribe({
      next: (periods) => {
        this.periods.set(periods);
        const open = periods.find((p) => p.status === 'OPEN') ?? periods[0] ?? null;
        this.selectedPeriodId.set(open?.id ?? null);
        this.loading.set(false);
        if (open) this.loadEvaluations(open.id);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('eval.errorLoad');
      },
    });
  }

  onPeriodChange(raw: string | number): void {
    if (!this.confirmDiscardDraft()) return;
    const id = typeof raw === 'number' ? raw : Number(raw);
    if (!id) return;
    this.selectedPeriodId.set(id);
    this.activeEval.set(null);
    this.draftBaseline = null;
    this.loadEvaluations(id);
  }

  loadEvaluations(periodId: number): void {
    this.loading.set(true);
    this.api.getKpiEvaluations(periodId).subscribe({
      next: (list) => {
        this.evaluations.set(list);
        this.loading.set(false);
        if (!this.canManage()) this.openOwnScorecard(list);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('eval.errorLoad');
      },
    });
  }

  openEmployee(row: EmployeeEvalRow): void {
    const periodId = this.selectedPeriodId();
    if (!periodId || !this.canManage()) return;
    if (!this.confirmDiscardDraft()) return;

    this.detailLoading.set(true);
    this.actionError.set(null);

    const prep$ = row.evaluation
      ? of(row.evaluation)
      : this.api.ensureKpiEvaluation(periodId, row.userId);

    prep$.pipe(finalize(() => this.detailLoading.set(false))).subscribe({
      next: (ev) => this.setActive(ev),
      error: () => this.actionError.set('eval.errorOpen'),
    });
  }

  backToList(): void {
    if (!this.confirmDiscardDraft()) return;
    this.activeEval.set(null);
    this.managerScores.set({});
    this.managerNotes.set('');
    this.actionError.set(null);
    this.draftBaseline = null;
    const id = this.selectedPeriodId();
    if (id) this.loadEvaluations(id);
  }

  hasUnsavedChanges(): boolean {
    if (!this.isCalculatedDraft() || !this.canManage()) return false;
    return isFormDirty(this.draftSnapshot(), this.draftBaseline);
  }

  private draftSnapshot(): { scores: Record<number, number>; notes: string } {
    return { scores: this.managerScores(), notes: this.managerNotes() };
  }

  private confirmDiscardDraft(): boolean {
    return allowLeaveIfClean(this.hasUnsavedChanges(), this.lang);
  }

  setManagerScore(resultId: number, raw: string | number): void {
    const n = typeof raw === 'number' ? raw : Number(raw);
    if (Number.isNaN(n)) return;
    const clamped = Math.max(0, Math.min(100, n));
    this.managerScores.update((m) => ({ ...m, [resultId]: clamped }));
  }

  calculate(): void {
    const ev = this.activeEval();
    if (!ev || ev.status !== 'DRAFT') return;
    this.saving.set(true);
    this.actionError.set(null);
    this.api.calculateKpiEvaluation(ev.id).subscribe({
      next: (updated) => {
        this.setActive(updated);
        this.saving.set(false);
      },
      error: () => {
        this.saving.set(false);
        this.actionError.set('eval.errorCalculate');
      },
    });
  }

  submitForReview(): void {
    const ev = this.activeEval();
    if (!ev || ev.status !== 'DRAFT' || (ev.results?.length ?? 0) === 0) return;
    this.persistDraft(ev, true);
  }

  startReview(): void {
    const ev = this.activeEval();
    if (!ev || ev.status !== 'SUBMITTED' || !this.canApprove()) return;
    this.saving.set(true);
    this.actionError.set(null);
    this.api.startKpiReview(ev.id).subscribe({
      next: (updated) => {
        this.setActive(updated);
        this.saving.set(false);
      },
      error: () => {
        this.saving.set(false);
        this.actionError.set('eval.errorReview');
      },
    });
  }

  approve(): void {
    const ev = this.activeEval();
    if (!ev || !this.canApprove()) return;
    if (ev.status !== 'IN_REVIEW') return;
    this.saving.set(true);
    this.actionError.set(null);
    this.api.approveKpiEvaluation(ev.id).subscribe({
      next: (updated) => {
        this.setActive(updated);
        this.saving.set(false);
      },
      error: () => {
        this.saving.set(false);
        this.actionError.set('eval.errorApprove');
      },
    });
  }

  reject(): void {
    const ev = this.activeEval();
    if (!ev || !this.canApprove() || ev.status !== 'IN_REVIEW') return;
    const reason = window.prompt(this.langOrFallback('eval.rejectPrompt'));
    if (reason == null) return;
    if (!reason.trim()) {
      this.actionError.set('eval.errorRejectReason');
      return;
    }
    this.saving.set(true);
    this.actionError.set(null);
    this.api.rejectKpiEvaluation(ev.id, reason.trim()).subscribe({
      next: (updated) => {
        this.setActive(updated);
        this.saving.set(false);
      },
      error: () => {
        this.saving.set(false);
        this.actionError.set('eval.errorReject');
      },
    });
  }

  private langOrFallback(key: string): string {
    return this.lang.t(key);
  }

  displayScore(result: EmployeeKpiResult): string {
    const local = this.managerScores()[result.id];
    if (local != null && isManagerKpi(result)) return String(local);
    const score = result.score;
    if (isSystemKpi(result)) {
      return `${this.formatPct(score)}%`;
    }
    return score % 1 === 0 ? String(score) : score.toFixed(1);
  }

  formatPct(n: number): string {
    return formatKpiNumber(n, 1);
  }

  overallLabel(): string {
    return formatKpiNumber(this.liveOverall(), 1);
  }

  private openOwnScorecard(list: EmployeeEvaluation[]): void {
    const me = this.currentUser.profile()?.id;
    if (me == null) return;
    const mine = list.find((e) => e.userId === me) ?? null;
    if (!mine) {
      this.activeEval.set(null);
      return;
    }
    if (!isEmployeeVisibleStatus(mine.status) || (mine.results?.length ?? 0) === 0) {
      this.activeEval.set(mine);
      return;
    }
    this.setActive(mine);
  }

  private persistDraft(ev: EmployeeEvaluation, submitAfter: boolean): void {
    const scores = this.managerScores();
    const notes = this.managerNotes();
    const patches = ev.results
      .filter(isManagerKpi)
      .map((r) => {
        const score = scores[r.id] ?? r.score;
        return this.api.adjustKpiResult(r.id, { score, adjustedValue: score });
      });

    this.saving.set(true);
    this.actionError.set(null);

    const afterScores$ =
      patches.length === 0
        ? of(ev)
        : forkJoin(patches).pipe(switchMap((list) => of(list[list.length - 1] ?? ev)));

    afterScores$
      .pipe(
        switchMap((latest) =>
          this.api.updateKpiEvaluationNotes(latest.id, notes || null).pipe(
            catchError(() => of(latest)),
          ),
        ),
        switchMap((latest) =>
          submitAfter ? this.api.submitKpiEvaluation(latest.id) : of(latest),
        ),
        finalize(() => this.saving.set(false)),
      )
      .subscribe({
        next: (updated) => this.setActive(updated),
        error: () =>
          this.actionError.set(submitAfter ? 'eval.errorSubmit' : 'eval.errorSave'),
      });
  }

  private setActive(ev: EmployeeEvaluation): void {
    this.activeEval.set(ev);
    const scores: Record<number, number> = {};
    for (const r of ev.results.filter(isManagerKpi)) {
      scores[r.id] = r.score;
    }
    this.managerScores.set(scores);
    this.managerNotes.set(ev.notes ?? '');
    this.draftBaseline = formSnapshot({ scores, notes: ev.notes ?? '' });
    this.evaluations.update((list) => {
      const idx = list.findIndex((e) => e.id === ev.id);
      if (idx < 0) return [ev, ...list];
      const copy = [...list];
      copy[idx] = ev;
      return copy;
    });
  }
}
