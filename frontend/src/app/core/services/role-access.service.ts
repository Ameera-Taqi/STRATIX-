import { Injectable, computed, inject } from '@angular/core';
import { canAccessModule, STRATIX_MODULES, SystemModuleCode } from '../config/stratix-modules';
import { CurrentUserService } from './current-user.service';

@Injectable({ providedIn: 'root' })
export class RoleAccessService {
  private readonly currentUser = inject(CurrentUserService);

  readonly role = computed(() => this.currentUser.profile().roleCode ?? 'ADMIN');

  canRead(moduleCode: SystemModuleCode): boolean {
    const module = STRATIX_MODULES.find((m) => m.code === moduleCode);
    if (!module) return false;
    return canAccessModule(module, this.role(), 'read');
  }

  canWrite(moduleCode: SystemModuleCode): boolean {
    const module = STRATIX_MODULES.find((m) => m.code === moduleCode);
    if (!module) return false;
    return canAccessModule(module, this.role(), 'write');
  }
}
