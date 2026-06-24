import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { getAuthToken } from '../services/auth-token.storage';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith(environment.apiUrl)) {
    return next(req);
  }

  const isPublic =
    req.url.endsWith('/health') ||
    req.url.includes('/auth/login') ||
    req.url.includes('/auth/forgot-password') ||
    req.url.includes('/auth/reset-password');
  if (isPublic) {
    return next(req);
  }

  const token = getAuthToken();
  if (!token) {
    return next(req);
  }

  return next(
    req.clone({
      setHeaders: { Authorization: `Bearer ${token}` },
    }),
  );
};
