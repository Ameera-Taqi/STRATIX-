import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiErrorService } from '../services/api-error.service';
import { CorrelationIdService } from '../services/correlation-id.service';

function isApiRequest(url: string): boolean {
  if (url.startsWith(environment.apiUrl)) return true;
  try {
    const path = url.includes('://') ? new URL(url).pathname : url;
    return path === '/api' || path.startsWith('/api/');
  } catch {
    return url.includes('/api/');
  }
}

function isQuietAuthUrl(url: string): boolean {
  return (
    url.includes('/auth/login') ||
    url.includes('/auth/register') ||
    url.includes('/auth/refresh') ||
    url.includes('/auth/logout')
  );
}

/** Surface non-auth API failures globally (keeps Correlation ID for support). */
export const apiErrorInterceptor: HttpInterceptorFn = (req, next) => {
  if (!isApiRequest(req.url)) {
    return next(req);
  }

  const errors = inject(ApiErrorService);
  const correlation = inject(CorrelationIdService);

  return next(req).pipe(
    catchError((err: unknown) => {
      if (err instanceof HttpErrorResponse && !isQuietAuthUrl(req.url)) {
        const responseCorr =
          err.headers?.get('X-Correlation-ID') ?? correlation.last();
        if (err.status === 0) {
          errors.publish({
            messageKey: 'errors.network',
            status: 0,
            correlationId: responseCorr,
          });
        } else if (err.status === 403) {
          errors.publish({
            messageKey: 'errors.forbidden',
            status: 403,
            correlationId: responseCorr,
          });
        } else if (err.status >= 500) {
          errors.publish({
            messageKey: 'errors.server',
            status: err.status,
            correlationId: responseCorr,
            detail: typeof err.error?.message === 'string' ? err.error.message : undefined,
          });
        } else if (err.status === 404 && req.method !== 'GET') {
          errors.publish({
            messageKey: 'errors.notFound',
            status: 404,
            correlationId: responseCorr,
          });
        }
      }
      return throwError(() => err);
    }),
  );
};
