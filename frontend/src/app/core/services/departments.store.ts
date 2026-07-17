import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { DepartmentRow } from '../models/department.model';

@Injectable({ providedIn: 'root' })
export class DepartmentsStore {
  private readonly api = inject(ApiService);

  readonly departments = signal<DepartmentRow[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly saving = signal(false);

  async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const rows = await firstValueFrom(this.api.getDepartments());
      this.departments.set([...rows].sort((a, b) => a.name.localeCompare(b.name)));
    } catch {
      this.error.set('roles.deptLoadFailed');
    } finally {
      this.loading.set(false);
    }
  }

  async create(name: string, description: string | null): Promise<boolean> {
    this.saving.set(true);
    this.error.set(null);
    try {
      const created = await firstValueFrom(
        this.api.createDepartment({ name: name.trim(), description: description?.trim() || null }),
      );
      this.departments.update((list) => [...list, created].sort((a, b) => a.name.localeCompare(b.name)));
      return true;
    } catch {
      this.error.set('roles.deptSaveFailed');
      return false;
    } finally {
      this.saving.set(false);
    }
  }

  async update(id: number, name: string, description: string | null): Promise<boolean> {
    this.saving.set(true);
    this.error.set(null);
    try {
      const updated = await firstValueFrom(
        this.api.updateDepartment(id, { name: name.trim(), description: description?.trim() || null }),
      );
      this.departments.update((list) =>
        list.map((d) => (d.id === id ? updated : d)).sort((a, b) => a.name.localeCompare(b.name)),
      );
      return true;
    } catch {
      this.error.set('roles.deptSaveFailed');
      return false;
    } finally {
      this.saving.set(false);
    }
  }

  async remove(id: number): Promise<boolean> {
    this.saving.set(true);
    this.error.set(null);
    try {
      await firstValueFrom(this.api.deleteDepartment(id));
      this.departments.update((list) => list.filter((d) => d.id !== id));
      return true;
    } catch {
      this.error.set('roles.deptDeleteFailed');
      return false;
    } finally {
      this.saving.set(false);
    }
  }
}
