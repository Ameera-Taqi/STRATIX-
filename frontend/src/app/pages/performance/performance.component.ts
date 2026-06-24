import { Component, computed, inject, OnInit } from '@angular/core';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { EmployeesStore } from '../../core/services/employees.store';
import { PerformanceMetricsService } from '../../core/services/performance-metrics.service';
import { KpiCardComponent } from '../../shared/components/kpi-card.component';
import { TranslatePipe } from '../../core/i18n/translate.pipe';

@Component({
  selector: 'app-performance',
  standalone: true,
  imports: [TopbarComponent, KpiCardComponent, TranslatePipe],
  templateUrl: './performance.component.html',
})
export class PerformanceComponent implements OnInit {
  private readonly employeesStore = inject(EmployeesStore);
  private readonly performance = inject(PerformanceMetricsService);

  readonly employees = this.employeesStore.employees;
  readonly metrics = this.performance.metrics;
  readonly loading = this.performance.loading;
  readonly error = this.performance.error;

  readonly isEmpty = computed(
    () =>
      !this.loading() &&
      !this.error() &&
      !this.metrics().hasData &&
      this.employees().length === 0,
  );

  readonly completionTone = computed(() =>
    this.metrics().completionRate >= 70 ? 'success' : 'primary',
  );

  readonly onTimeTone = computed(() =>
    this.metrics().onTimePct >= 80 ? 'primary' : 'warning',
  );

  readonly overdueTone = computed(() =>
    this.metrics().overdueTasks === 0 ? 'success' : 'danger',
  );

  readonly productivityTone = computed(() =>
    this.metrics().teamProductivity >= 70 ? 'success' : 'primary',
  );

  ngOnInit(): void {
    this.performance.ensureData();
  }

  retry(): void {
    this.performance.reload();
  }
}
