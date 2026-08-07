import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { environment } from '../../../environments/environment';
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

function newCorrelationId(): string {
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
    return crypto.randomUUID();
  }
  return `corr-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
}

/** Attach X-Correlation-ID on every API call and remember the last id for error UI. */
export const correlationInterceptor: HttpInterceptorFn = (req, next) => {
  if (!isApiRequest(req.url)) {
    return next(req);
  }

  const correlation = inject(CorrelationIdService);
  const id = req.headers.get('X-Correlation-ID') ?? newCorrelationId();
  correlation.setLast(id);

  return next(
    req.clone({
      setHeaders: { 'X-Correlation-ID': id },
    }),
  );
};
