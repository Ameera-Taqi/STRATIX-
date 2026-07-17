import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import {
  CreateOrganizationRoleRequest,
  OrganizationRoleRow,
  UpdateOrganizationRoleRequest,
} from '../models/organization-role.model';

@Injectable({ providedIn: 'root' })
export class OrganizationRolesStore {
  private readonly api = inject(ApiService);

  readonly roles = signal<OrganizationRoleRow[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly saving = signal(false);

  async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const rows = await firstValueFrom(this.api.getOrganizationRoles());
      this.roles.set([...rows].sort((a, b) => a.name.localeCompare(b.name)));
    } catch {
      this.error.set('roles.customLoadFailed');
    } finally {
      this.loading.set(false);
    }
  }

  async create(body: CreateOrganizationRoleRequest): Promise<boolean> {
    this.saving.set(true);
    this.error.set(null);
    try {
      const created = await firstValueFrom(this.api.createOrganizationRole(body));
      this.roles.update((list) => [...list, created].sort((a, b) => a.name.localeCompare(b.name)));
      return true;
    } catch (err: unknown) {
      this.error.set(this.mapError(err, 'roles.customSaveFailed'));
      return false;
    } finally {
      this.saving.set(false);
    }
  }

  async update(id: number, body: UpdateOrganizationRoleRequest): Promise<boolean> {
    this.saving.set(true);
    this.error.set(null);
    try {
      const updated = await firstValueFrom(this.api.updateOrganizationRole(id, body));
      this.roles.update((list) =>
        list.map((r) => (r.id === id ? updated : r)).sort((a, b) => a.name.localeCompare(b.name)),
      );
      return true;
    } catch (err: unknown) {
      this.error.set(this.mapError(err, 'roles.customSaveFailed'));
      return false;
    } finally {
      this.saving.set(false);
    }
  }

  async remove(id: number): Promise<boolean> {
    this.saving.set(true);
    this.error.set(null);
    try {
      await firstValueFrom(this.api.deleteOrganizationRole(id));
      this.roles.update((list) => list.filter((r) => r.id !== id));
      return true;
    } catch {
      this.error.set('roles.customDeleteFailed');
      return false;
    } finally {
      this.saving.set(false);
    }
  }

  private mapError(err: unknown, fallback: string): string {
    const message = (err as { error?: { message?: string } })?.error?.message ?? '';
    const lower = message.toLowerCase();
    if (lower.includes('reserved')) return 'roles.customCodeReserved';
    if (lower.includes('already exists')) return 'roles.customCodeTaken';
    if (lower.includes('invalid base')) return 'roles.customBaseInvalid';
    if (lower.includes('required')) return 'roles.customNameRequired';
    return fallback;
  }
}
