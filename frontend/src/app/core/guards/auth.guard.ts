import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { OnboardingService } from '../services/onboarding.service';

/** Requires a restored session with a real profile (no default Admin). */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.sessionReady()) {
    // APP_INITIALIZER should have completed restore; fail closed to login.
    return router.createUrlTree(['/auth/login']);
  }

  if (auth.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/auth/login']);
};

export const guestGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const onboarding = inject(OnboardingService);

  if (!auth.sessionReady()) {
    return true;
  }

  if (auth.isAuthenticated()) {
    const path = await onboarding.resolveHomePath();
    return router.createUrlTree([path]);
  }

  return true;
};
