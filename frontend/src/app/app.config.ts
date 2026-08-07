import { ApplicationConfig, inject, provideAppInitializer, provideZoneChangeDetection } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { authRefreshInterceptor } from './core/interceptors/auth-refresh.interceptor';
import { correlationInterceptor } from './core/interceptors/correlation.interceptor';
import { apiErrorInterceptor } from './core/interceptors/api-error.interceptor';
import { routes } from './app.routes';
import { ThemeService } from './core/theme/theme.service';
import { AuthService } from './core/services/auth.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(
      withInterceptors([
        correlationInterceptor,
        authInterceptor,
        authRefreshInterceptor,
        apiErrorInterceptor,
      ]),
    ),
    provideAppInitializer(() => {
      inject(ThemeService);
      // Block first navigation until session restore (+ CMS gate) completes.
      return firstValueFrom(inject(AuthService).restoreSession());
    }),
  ],
};
