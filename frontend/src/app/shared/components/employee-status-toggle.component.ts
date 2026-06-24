import { Component, input, output } from '@angular/core';
import { EmployeeStatus } from '../../core/data/mock-data';
import { TranslatePipe } from '../../core/i18n/translate.pipe';

@Component({
  selector: 'app-employee-status-toggle',
  standalone: true,
  imports: [TranslatePipe],
  template: `
    <button
      type="button"
      role="switch"
      class="stratix-switch"
      [class.stratix-switch--compact]="compact()"
      [attr.aria-checked]="isActive()"
      [attr.aria-label]="'team.changeStatus' | t"
      (click)="toggle()"
    >
      <span class="stratix-switch-track" [class.stratix-switch-track--on]="isActive()">
        <span class="stratix-switch-thumb"></span>
      </span>
      @if (showLabel()) {
        <span class="stratix-switch-label" [class.text-emerald-600]="isActive()" [class.dark:text-emerald-400]="isActive()">
          {{ isActive() ? ('team.statusActive' | t) : ('team.statusInactive' | t) }}
        </span>
      }
    </button>
  `,
})
export class EmployeeStatusToggleComponent {
  readonly status = input.required<EmployeeStatus>();
  readonly compact = input(false);
  readonly showLabel = input(true);
  readonly statusChange = output<EmployeeStatus>();

  isActive(): boolean {
    return this.status() === 'Active';
  }

  toggle(): void {
    this.statusChange.emit(this.isActive() ? 'Inactive' : 'Active');
  }
}
