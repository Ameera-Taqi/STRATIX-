import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import {
  CompanyAdminModulePermission,
  UpdateCompanyAdminModulePermissionItem,
} from '../models/cms.model';

export type CmsPermissionMap = Record<string, { visible: boolean; writable: boolean }>;

@Injectable({ providedIn: 'root' })
export class CmsPermissionsStore {
  private readonly api = inject(ApiService);

  readonly permissions = signal<CompanyAdminModulePermission[]>([]);
  readonly map = signal<CmsPermissionMap>({});
  readonly loading = signal(false);
  readonly loaded = signal(false);
  readonly error = signal<string | null>(null);
  readonly saving = signal(false);

  readonly visibleCount = computed(() => this.permissions().filter((p) => p.visibleToCompanyAdmin).length);

  async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const rows = await firstValueFrom(this.api.getCompanyAdminPermissions());
      this.apply(rows);
      this.loaded.set(true);
    } catch {
      this.error.set('cms.loadFailed');
    } finally {
      this.loading.set(false);
    }
  }

  async save(modules: UpdateCompanyAdminModulePermissionItem[]): Promise<boolean> {
    this.saving.set(true);
    this.error.set(null);
    try {
      const rows = await firstValueFrom(this.api.updateCompanyAdminPermissions({ modules }));
      this.apply(rows);
      return true;
    } catch {
      this.error.set('cms.saveFailed');
      return false;
    } finally {
      this.saving.set(false);
    }
  }

  clear(): void {
    this.permissions.set([]);
    this.map.set({});
    this.loaded.set(false);
    this.error.set(null);
  }

  private apply(rows: CompanyAdminModulePermission[]): void {
    this.permissions.set(rows);
    const next: CmsPermissionMap = {};
    for (const row of rows) {
      next[row.moduleCode] = {
        visible: row.visibleToCompanyAdmin,
        writable: row.writableByCompanyAdmin,
      };
    }
    this.map.set(next);
  }
}
