import { Injectable, inject } from '@angular/core';
import { CmsPermissionsStore } from './cms-permissions.store';
import { BrandingStore } from './branding.store';

/**
 * Session-scoped tenant gate data only (CMS + branding).
 * Domain lists (projects, tasks, …) load on their pages.
 */
@Injectable({ providedIn: 'root' })
export class DataBootstrapService {
  private readonly cmsStore = inject(CmsPermissionsStore);
  private readonly brandingStore = inject(BrandingStore);

  /** @deprecated Prefer awaiting AuthService login/restore which already loads CMS. */
  bootstrap(): void {
    void this.bootstrapSessionData();
  }

  async bootstrapSessionData(): Promise<void> {
    await Promise.all([this.cmsStore.load(), this.brandingStore.load()]);
  }
}
