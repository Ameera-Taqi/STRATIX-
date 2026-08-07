import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { getAuthToken } from '../services/auth-token.storage';
import { environment } from '../../../environments/environment';

function isApiRequest(url: string): boolean {
  if (url.startsWith(environment.apiUrl)) return true;
  try {
    const path = url.includes('://') ? new URL(url).pathname : url;
    return path === '/api' || path.startsWith('/api/');
  } catch {
    return url.includes('/api/');
  }
}

function isAuthSessionUrl(url: string): boolean {
  return (
    url.includes('/auth/login') ||
    url.includes('/auth/register') ||
    url.includes('/auth/refresh') ||
    url.includes('/auth/logout') ||
    url.includes('/auth/forgot-password') ||
    url.includes('/auth/reset-password')
  );
}

/** On 401, attempt one refresh + retry; otherwise force logout. */
export const authRefreshInterceptor: HttpInterceptorFn = (req, next) => {
  if (!isApiRequest(req.url) || isAuthSessionUrl(req.url)) {
    return next(req);
  }

  return next(req).pipe(
    catchError((err: unknown) => {
      if (!(err instanceof HttpErrorResponse) || err.status !== 401) {
        return throwError(() => err);
      }

      const auth = inject(AuthService);
      return auth.refreshAccessToken().pipe(
        switchMap((token) => {
          if (!token) {
            auth.forceLogout();
            return throwError(() => err);
          }
          const access = getAuthToken() ?? token;
          return next(
            req.clone({
              setHeaders: { Authorization: `Bearer ${access}` },
            }),
          );
        }),
      );
    }),
  );
};
