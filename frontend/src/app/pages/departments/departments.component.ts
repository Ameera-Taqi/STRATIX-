import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { RoleAccessService } from '../../core/services/role-access.service';
import { DepartmentsStore } from '../../core/services/departments.store';
import { EmployeesStore } from '../../core/services/employees.store';
import { UiIconComponent } from '../../shared/components/ui-icon/ui-icon.component';

@Component({
  selector: 'app-departments',
  standalone: true,
  imports: [TopbarComponent, TranslatePipe, FormsModule, UiIconComponent],
  templateUrl: './departments.component.html',
})
export class DepartmentsComponent implements OnInit {
  private readonly roleAccess = inject(RoleAccessService);
  private readonly departmentsStore = inject(DepartmentsStore);
  private readonly employeesStore = inject(EmployeesStore);

  readonly canWrite = computed(() => this.roleAccess.canWrite('DEPARTMENTS'));
  readonly departments = this.departmentsStore.departments;
  readonly loading = this.departmentsStore.loading;
  readonly error = this.departmentsStore.error;
  readonly saving = this.departmentsStore.saving;

  readonly showModal = signal(false);
  readonly editingId = signal<number | null>(null);
  readonly formError = signal<string | null>(null);
  form = { name: '', description: '' };

  ngOnInit(): void {
    void this.departmentsStore.load();
  }

  openAdd(): void {
    this.editingId.set(null);
    this.form = { name: '', description: '' };
    this.formError.set(null);
    this.showModal.set(true);
  }

  openEdit(id: number, name: string, description: string | null): void {
    this.editingId.set(id);
    this.form = { name, description: description ?? '' };
    this.formError.set(null);
    this.showModal.set(true);
  }

  closeModal(): void {
    this.showModal.set(false);
    this.formError.set(null);
  }

  async save(): Promise<void> {
    if (!this.form.name.trim()) {
      this.formError.set('roles.deptNameRequired');
      return;
    }
    const id = this.editingId();
    const ok =
      id == null
        ? await this.departmentsStore.create(this.form.name, this.form.description)
        : await this.departmentsStore.update(id, this.form.name, this.form.description);
    if (ok) {
      this.employeesStore.reloadDepartments();
      this.closeModal();
    } else {
      this.formError.set(this.departmentsStore.error());
    }
  }

  async remove(id: number): Promise<void> {
    const ok = await this.departmentsStore.remove(id);
    if (ok) this.employeesStore.reloadDepartments();
  }
}
