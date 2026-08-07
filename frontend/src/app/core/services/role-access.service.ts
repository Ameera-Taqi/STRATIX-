import { Injectable, computed, inject } from '@angular/core';
import { canAccessModule, STRATIX_MODULES, SystemModuleCode } from '../config/stratix-modules';
import { UserRole } from '../models/user.model';
import { CurrentUserService } from './current-user.service';
import { CmsPermissionsStore } from './cms-permissions.store';

@Injectable({ providedIn: 'root' })
export class RoleAccessService {
  private readonly currentUser = inject(CurrentUserService);
  private readonly cms = inject(CmsPermissionsStore);

  readonly role = computed<UserRole | null>(() => this.currentUser.profile()?.roleCode ?? null);

  /** Recompute when CMS map or role changes (sidebar uses computed). */
  readonly cmsMap = computed(() => this.cms.map());

  canRead(moduleCode: SystemModuleCode): boolean {
    const role = this.role();
    if (!role) return false;
    const module = STRATIX_MODULES.find((m) => m.code === moduleCode);
    if (!module) return false;
    return canAccessModule(module, role, 'read', this.cmsMap());
  }

  canWrite(moduleCode: SystemModuleCode): boolean {
    const role = this.role();
    if (!role) return false;
    const module = STRATIX_MODULES.find((m) => m.code === moduleCode);
    if (!module) return false;
    return canAccessModule(module, role, 'write', this.cmsMap());
  }
}
