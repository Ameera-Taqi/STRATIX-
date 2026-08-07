import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { map } from 'rxjs';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { RisksStore } from '../../core/services/risks.store';
import { RoleAccessService } from '../../core/services/role-access.service';
import { RiskImpact } from '../../core/models/risk.model';
import { riskLevelClass, riskStatusClass } from '../../shared/utils/risk.util';
import {
  riskImpactLabelKey,
  riskLevelLabelKey,
  riskProbabilityLabelKey,
  riskStatusLabelKey,
} from '../../shared/utils/enum-labels';
import { UiIconComponent } from '../../shared/components/ui-icon/ui-icon.component';
import {
  BreadcrumbItem,
  BreadcrumbsComponent,
} from '../../shared/components/breadcrumbs/breadcrumbs.component';

@Component({
  selector: 'app-risk-detail',
  standalone: true,
  imports: [TopbarComponent, RouterLink, TranslatePipe, DatePipe, UiIconComponent, FormsModule, BreadcrumbsComponent],
  templateUrl: './risk-detail.component.html',
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
export class RiskDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly store = inject(RisksStore);
  private readonly roleAccess = inject(RoleAccessService);

  private readonly riskId = toSignal(
    this.route.paramMap.pipe(map((params) => Number(params.get('id')))),
    { initialValue: Number(this.route.snapshot.paramMap.get('id')) },
  );

  readonly risk = computed(() => this.store.getById(this.riskId()));
  readonly canWrite = computed(() => this.roleAccess.canWrite('RISKS'));

  readonly breadcrumbs = computed((): BreadcrumbItem[] => {
    const r = this.risk();
    const crumbs: BreadcrumbItem[] = [{ labelKey: 'nav.risks', link: '/risks' }];
    if (r?.projectId && r.projectName) {
      crumbs.push({ label: r.projectName, link: ['/projects', r.projectId] });
    }
    if (r) {
      crumbs.push({ label: r.title });
    }
    return crumbs;
  });

  readonly riskLevelClass = riskLevelClass;
  readonly riskStatusClass = riskStatusClass;
  readonly riskLevelKey = riskLevelLabelKey;
  readonly riskStatusKey = riskStatusLabelKey;
  readonly riskImpactKey = riskImpactLabelKey;
  readonly riskProbabilityKey = riskProbabilityLabelKey;
  readonly residualOptions: Array<RiskImpact | null> = [null, 'LOW', 'MEDIUM', 'HIGH'];

  readonly showCloseModal = signal(false);
  readonly closeError = signal<string | null>(null);
  closeForm = {
    closureReason: '',
    residualRisk: null as RiskImpact | null,
  };

  openCloseRisk(): void {
    if (!this.canWrite()) return;
    const risk = this.risk();
    if (!risk || risk.status === 'CLOSED') return;
    this.closeError.set(null);
    this.closeForm = { closureReason: '', residualRisk: null };
    this.showCloseModal.set(true);
  }

  cancelCloseRisk(): void {
    this.showCloseModal.set(false);
    this.closeError.set(null);
  }

  confirmCloseRisk(): void {
    const risk = this.risk();
    if (!risk) return;
    if (!this.closeForm.closureReason.trim()) {
      this.closeError.set('risks.closureReasonRequired');
      return;
    }
    this.store.closeRisk(risk.id, {
      closureReason: this.closeForm.closureReason,
      residualRisk: this.closeForm.residualRisk,
    });
    this.cancelCloseRisk();
  }
}
