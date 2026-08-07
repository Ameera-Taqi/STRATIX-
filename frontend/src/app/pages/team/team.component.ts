import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import {
  DEFAULT_EMPLOYEE_PASSWORD,
  EmployeeStatus,
  EmployeesStore,
} from '../../core/services/employees.store';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { EmployeeStatusToggleComponent } from '../../shared/components/employee-status-toggle.component';
import { departmentBadgeClass, employeeInitials } from '../../shared/utils/employee.util';
import { UiIconComponent } from '../../shared/components/ui-icon/ui-icon.component';
import { RoleAccessService } from '../../core/services/role-access.service';
import { assignableRolesFor, roleLabelKey } from '../../core/config/stratix-roles';
import { UserRole } from '../../core/models/user.model';
import { LanguageService } from '../../core/i18n/language.service';

@Component({
  selector: 'app-team',
  standalone: true,
  imports: [TopbarComponent, TranslatePipe, FormsModule, RouterLink, EmployeeStatusToggleComponent, UiIconComponent],
  templateUrl: './team.component.html',
})
export class TeamComponent implements OnInit {
  private readonly store = inject(EmployeesStore);
  private readonly roleAccess = inject(RoleAccessService);
  private readonly i18n = inject(LanguageService);

  readonly canWrite = computed(() => this.roleAccess.canWrite('EMPLOYEES'));
  readonly roleOptions = computed(() => assignableRolesFor(this.roleAccess.role()));

  readonly employees = this.store.employees;
  readonly departments = this.store.departmentOptions;
  readonly search = signal('');
  readonly showAddModal = signal(false);
  readonly showCredentials = signal(false);
  readonly createdCredentials = signal<{ email: string; password: string } | null>(null);
  readonly formError = signal<string | null>(null);
  readonly submitting = signal(false);

  form = {
    name: '',
    email: '',
    role: 'EMPLOYEE' as UserRole,
    department: 'IT',
    status: 'Active' as EmployeeStatus,
  };

  readonly filtered = computed(() => {
    const q = this.search().toLowerCase().trim();
    this.i18n.lang();
    return this.employees().filter((e) => {
      if (!q) return true;
      const roleText = this.i18n.t(roleLabelKey(e.role)).toLowerCase();
      return (
        e.name.toLowerCase().includes(q) ||
        e.role.toLowerCase().includes(q) ||
        roleText.includes(q) ||
        e.department.toLowerCase().includes(q)
      );
    });
  });

  readonly stats = computed(() => {
    const list = this.employees();
    return {
      total: list.length,
      active: list.filter((e) => e.status === 'Active').length,
      inactive: list.filter((e) => e.status === 'Inactive').length,
    };
  });

  readonly initials = employeeInitials;
  readonly departmentBadge = departmentBadgeClass;
  readonly roleKey = roleLabelKey;

  openAddModal(): void {
    this.formError.set(null);
    const roles = this.roleOptions();
    this.form = {
      name: '',
      email: '',
      role: roles.includes('EMPLOYEE') ? 'EMPLOYEE' : (roles[0] ?? 'EMPLOYEE'),
      department: this.departments()[0] ?? 'IT',
      status: 'Active',
    };
    this.showAddModal.set(true);
  }

  closeAddModal(): void {
    this.showAddModal.set(false);
    this.formError.set(null);
  }

  closeCredentials(): void {
    this.showCredentials.set(false);
    this.createdCredentials.set(null);
  }

  saveEmployee(): void {
    if (!this.form.name.trim()) {
      this.formError.set('team.errorName');
      return;
    }
    this.submitting.set(true);
    this.formError.set(null);
    this.store.addEmployee(this.form).subscribe({
      next: (row) => {
        this.submitting.set(false);
        this.closeAddModal();
        this.createdCredentials.set({
          email:
            row.email?.trim() ||
            this.form.email.trim() ||
            `${row.name.replace(/\s+/g, '.').toLowerCase()}@stratix.local`,
          password: DEFAULT_EMPLOYEE_PASSWORD,
        });
        this.showCredentials.set(true);
      },
      error: () => {
        this.submitting.set(false);
        this.formError.set('team.errorSave');
      },
    });
  }

  setStatus(id: number, status: EmployeeStatus): void {
    if (!this.canWrite()) return;
    this.store.updateStatus(id, status).subscribe();
  }

  ngOnInit(): void {
    this.store.loadAdministrationFromApi();
  }
}
