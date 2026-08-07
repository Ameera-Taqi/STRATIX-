import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { RoleAccessService } from '../services/role-access.service';
import { SystemModuleCode } from '../config/stratix-modules';

/** Blocks a route unless the current role can read the given module. */
export function moduleGuard(moduleCode: SystemModuleCode): CanActivateFn {
  return () => {
    const roleAccess = inject(RoleAccessService);
    const router = inject(Router);

    if (roleAccess.canRead(moduleCode)) {
      return true;
    }

    return router.createUrlTree(['/forbidden'], {
      queryParams: { module: moduleCode },
    });
  };
}

/** Convenience alias matching the generic "roleGuard" naming used elsewhere in the app. */
export const roleGuard = moduleGuard;
