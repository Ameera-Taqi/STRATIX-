import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { getAuthToken } from '../services/auth-token.storage';

function isApiRequest(url: string): boolean {
  // HttpClient may pass a relative `/api/...` or an absolute `http://host/api/...` URL.
  if (url.startsWith(environment.apiUrl)) return true;
  try {
    const path = url.includes('://') ? new URL(url).pathname : url;
    return path === '/api' || path.startsWith('/api/');
  } catch {
    return url.includes('/api/');
  }
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (!isApiRequest(req.url)) {
    return next(req);
  }

  const isPublic =
    req.url.endsWith('/health') ||
    req.url.includes('/auth/login') ||
    req.url.includes('/auth/register') ||
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
