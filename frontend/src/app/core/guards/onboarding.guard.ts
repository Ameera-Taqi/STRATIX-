import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { CurrentUserService } from '../services/current-user.service';
import { OnboardingService } from '../services/onboarding.service';

/** Sends unfinished org admins to the guided setup wizard. */
export const requireOnboardingGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const currentUser = inject(CurrentUserService);
  const onboarding = inject(OnboardingService);

  if (!auth.isAuthenticated()) {
    return router.createUrlTree(['/auth/login']);
  }

  const role = currentUser.profile()?.roleCode;
  if (role !== 'ORG_ADMIN' && role !== 'ADMIN') {
    return router.createUrlTree(['/dashboard']);
  }

  const status = onboarding.status() ?? (await onboarding.load());
  if (status?.onboardingCompleted) {
    return router.createUrlTree(['/dashboard']);
  }

  return true;
};

/** Keep unfinished org admins out of the empty dashboard. */
export const blockDashboardUntilOnboardedGuard: CanActivateFn = async () => {
  const currentUser = inject(CurrentUserService);
  const onboarding = inject(OnboardingService);
  const router = inject(Router);

  const role = currentUser.profile()?.roleCode;
  if (role !== 'ORG_ADMIN' && role !== 'ADMIN') return true;

  const status = onboarding.status() ?? (await onboarding.load());
  if (status && !status.onboardingCompleted) {
    return router.createUrlTree(['/onboarding']);
  }

  return true;
};
