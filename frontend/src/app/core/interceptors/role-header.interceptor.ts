import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { CurrentUserService } from '../services/current-user.service';

export const roleHeaderInterceptor: HttpInterceptorFn = (req, next) => {
  const currentUser = inject(CurrentUserService);
  const profile = currentUser.profile();
  if (!profile?.roleCode) {
    return next(req);
  }
  return next(
    req.clone({
      setHeaders: {
        'X-Stratix-Role': profile.roleCode,
      },
    }),
  );
};
