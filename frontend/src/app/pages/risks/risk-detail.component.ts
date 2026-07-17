import { DatePipe } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { map } from 'rxjs';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { RisksStore } from '../../core/services/risks.store';
import { riskLevelClass, riskStatusClass } from '../../shared/utils/risk.util';
import {
  riskImpactLabelKey,
  riskLevelLabelKey,
  riskProbabilityLabelKey,
  riskStatusLabelKey,
} from '../../shared/utils/enum-labels';
import { UiIconComponent } from '../../shared/components/ui-icon/ui-icon.component';

@Component({
  selector: 'app-risk-detail',
  standalone: true,
  imports: [TopbarComponent, RouterLink, TranslatePipe, DatePipe, UiIconComponent],
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

  private readonly riskId = toSignal(
    this.route.paramMap.pipe(map((params) => Number(params.get('id')))),
    { initialValue: Number(this.route.snapshot.paramMap.get('id')) },
  );

  readonly risk = computed(() => this.store.getById(this.riskId()));
  readonly riskLevelClass = riskLevelClass;
  readonly riskStatusClass = riskStatusClass;
  readonly riskLevelKey = riskLevelLabelKey;
  readonly riskStatusKey = riskStatusLabelKey;
  readonly riskImpactKey = riskImpactLabelKey;
  readonly riskProbabilityKey = riskProbabilityLabelKey;
}
