import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  Observable,
  catchError,
  finalize,
  map,
  of,
  shareReplay,
  switchMap,
  tap,
} from 'rxjs';
import { AuthUserProfile, RegisterOrganizationRequest, UpdateMyProfileRequest } from '../models/auth.model';
import { UserRole } from '../models/user.model';
import { AuthApiService } from '../api/auth-api.service';
import { CurrentUserProfile, CurrentUserService } from './current-user.service';
import {
  clearAuthToken,
  getAuthToken,
  getRefreshToken,
  isRememberMeEnabled,
  setAuthTokens,
} from './auth-token.storage';
import { CmsPermissionsStore } from './cms-permissions.store';
import { BrandingStore } from './branding.store';
import { NotificationsStore } from './notifications.store';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly router = inject(Router);
  private readonly currentUser = inject(CurrentUserService);
  private readonly api = inject(AuthApiService);
  private readonly cms = inject(CmsPermissionsStore);
  private readonly branding = inject(BrandingStore);
  private readonly notifications = inject(NotificationsStore);

  private readonly _token = signal<string | null>(getAuthToken());
  private readonly _sessionReady = signal(false);
  private refreshInFlight: Observable<string | null> | null = null;

  readonly isAuthenticated = computed(() => this._token() !== null && this.currentUser.profile() !== null);
  readonly token = this._token.asReadonly();
  readonly sessionReady = this._sessionReady.asReadonly();

  login(username: string, password: string, remember = false): Observable<boolean> {
    return this.api.login({ username, password }).pipe(
      switchMap((response) => {
        this.applyAuthResponse(response.token, response.refreshToken, response.user, remember);
        return this.loadTenantGateData().pipe(map(() => true));
      }),
      catchError(() => of(false)),
    );
  }

  register(body: RegisterOrganizationRequest, remember = false): Observable<{ ok: boolean; error?: string }> {
    return this.api.registerOrganization(body).pipe(
      switchMap((response) => {
        this.applyAuthResponse(response.token, response.refreshToken, response.user, remember);
        return this.loadTenantGateData().pipe(map(() => ({ ok: true as const })));
      }),
      catchError((err) => of({ ok: false, error: err?.error?.message ?? 'auth.registerError' })),
    );
  }

  invalidateLocalSession(): void {
    this.clearSession();
  }

  prepareForSignIn(): void {
    this.invalidateLocalSession();
    this._sessionReady.set(true);
  }

  logout(): void {
    const refreshToken = getRefreshToken();
    const finish = (): void => {
      this.clearSession();
      this.cms.clear();
      this.branding.resetLocal();
      void this.router.navigate(['/auth/login']);
    };

    if (!refreshToken) {
      finish();
      return;
    }

    this.api
      .logout({ refreshToken })
      .pipe(
        catchError(() => of(void 0)),
        finalize(finish),
      )
      .subscribe();
  }

  /** Force local clear without calling backend (e.g. refresh reuse / invalid session). */
  forceLogout(): void {
    this.clearSession();
    this.cms.clear();
    this.branding.resetLocal();
    void this.router.navigate(['/auth/login']);
  }

  restoreSession(): Observable<void> {
    this._sessionReady.set(false);
    const token = getAuthToken();
    if (!token) {
      this.clearSession();
      this._sessionReady.set(true);
      return of(void 0);
    }

    this._token.set(token);
    return this.api.me().pipe(
      switchMap((user) => {
        this.currentUser.setProfile(this.toCurrentProfile(user));
        return this.loadTenantGateData();
      }),
      map(() => void 0),
      catchError(() => {
        // Access token expired — try refresh once before clearing.
        return this.refreshAccessToken().pipe(
          switchMap((access) => {
            if (!access) {
              this.clearSession();
              return of(void 0);
            }
            return this.api.me().pipe(
              switchMap((user) => {
                this.currentUser.setProfile(this.toCurrentProfile(user));
                return this.loadTenantGateData();
              }),
              catchError(() => {
                this.clearSession();
                return of(void 0);
              }),
            );
          }),
        );
      }),
      finalize(() => this._sessionReady.set(true)),
    );
  }

  /**
   * Exchange refresh token for a new access token. Concurrent callers share one in-flight request.
   */
  refreshAccessToken(): Observable<string | null> {
    if (this.refreshInFlight) {
      return this.refreshInFlight;
    }

    const refreshToken = getRefreshToken();
    if (!refreshToken) {
      return of(null);
    }

    this.refreshInFlight = this.api.refresh({ refreshToken }).pipe(
      map((response) => {
        const remember = isRememberMeEnabled();
        setAuthTokens(response.token, response.refreshToken ?? refreshToken, remember);
        this._token.set(response.token);
        if (response.user) {
          this.currentUser.setProfile(this.toCurrentProfile(response.user));
        }
        return response.token;
      }),
      catchError(() => {
        this.clearSession();
        return of(null);
      }),
      finalize(() => {
        this.refreshInFlight = null;
      }),
      shareReplay(1),
    );

    return this.refreshInFlight;
  }

  updateMyProfile(body: UpdateMyProfileRequest): Observable<boolean> {
    return this.api.updateMyProfile(body).pipe(
      tap((user) => this.currentUser.setProfile(this.toCurrentProfile(user))),
      map(() => true),
      catchError(() => of(false)),
    );
  }

  private loadTenantGateData(): Observable<void> {
    return new Observable<void>((subscriber) => {
      void Promise.all([this.cms.load(), this.branding.load()])
        .then(() => {
          this.notifications.refresh();
          subscriber.next();
          subscriber.complete();
        })
        .catch((err) => subscriber.error(err));
    }).pipe(catchError(() => of(void 0)));
  }

  private applyAuthResponse(
    token: string,
    refreshToken: string | null | undefined,
    user: AuthUserProfile,
    remember: boolean,
  ): void {
    setAuthTokens(token, refreshToken, remember);
    this._token.set(token);
    this.currentUser.setProfile(this.toCurrentProfile(user));
    this._sessionReady.set(true);
  }

  private clearSession(): void {
    clearAuthToken();
    this._token.set(null);
    this.currentUser.clearProfile();
    this.notifications.clear();
    this.refreshInFlight = null;
  }

  private toCurrentProfile(user: AuthUserProfile): CurrentUserProfile {
    return {
      id: user.id,
      name: user.name,
      email: user.email,
      role: user.role,
      roleCode: user.roleCode as UserRole,
      department: user.department,
      employeeId: user.employeeId,
    };
  }
}
