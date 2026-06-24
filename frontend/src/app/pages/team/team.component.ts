import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { EmployeeStatus, EmployeesStore } from '../../core/services/employees.store';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { EmployeeStatusToggleComponent } from '../../shared/components/employee-status-toggle.component';
import { departmentBadgeClass, employeeInitials } from '../../shared/utils/employee.util';

@Component({
  selector: 'app-team',
  standalone: true,
  imports: [TopbarComponent, TranslatePipe, FormsModule, RouterLink, EmployeeStatusToggleComponent],
  templateUrl: './team.component.html',
})
export class TeamComponent implements OnInit {
  private readonly store = inject(EmployeesStore);

  readonly employees = this.store.employees;
  readonly departments = this.store.departmentOptions;
  readonly search = signal('');
  readonly showAddModal = signal(false);
  readonly formError = signal<string | null>(null);
  readonly submitting = signal(false);
  readonly roleOptions = [
    'Project Manager',
    'Team Leader',
    'Employee',
    'Admin',
    'Executive Viewer',
  ];
  form = {
    name: '',
    email: '',
    role: 'Employee',
    department: 'IT',
    status: 'Active' as EmployeeStatus,
  };

  readonly filtered = computed(() => {
    const q = this.search().toLowerCase().trim();
    return this.employees().filter((e) => {
      if (!q) return true;
      return (
        e.name.toLowerCase().includes(q) ||
        e.role.toLowerCase().includes(q) ||
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

  openAddModal(): void {
    this.formError.set(null);
    this.form = {
      name: '',
      email: '',
      role: 'Employee',
      department: 'IT',
      status: 'Active',
    };
    this.showAddModal.set(true);
  }

  closeAddModal(): void {
    this.showAddModal.set(false);
    this.formError.set(null);
  }

  saveEmployee(): void {
    if (!this.form.name.trim()) {
      this.formError.set('team.errorName');
      return;
    }
    this.submitting.set(true);
    this.formError.set(null);
    this.store.addEmployee(this.form).subscribe({
      next: () => {
        this.submitting.set(false);
        this.closeAddModal();
      },
      error: () => {
        this.submitting.set(false);
        this.formError.set('team.errorSave');
      },
    });
  }

  setStatus(id: number, status: EmployeeStatus): void {
    this.store.updateStatus(id, status).subscribe();
  }

  ngOnInit(): void {
    if (!this.store.loaded()) {
      this.store.loadFromApi();
    }
  }
}
