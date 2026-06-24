import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { AuthUserProfile, UpdateMyProfileRequest } from '../models/auth.model';
import { UserRole } from '../models/user.model';
import { ApiService } from './api.service';
import { CurrentUserProfile, CurrentUserService } from './current-user.service';
import { clearAuthToken, getAuthToken, setAuthToken } from './auth-token.storage';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly router = inject(Router);
  private readonly currentUser = inject(CurrentUserService);
  private readonly api = inject(ApiService);

  private readonly _token = signal<string | null>(getAuthToken());

  readonly isAuthenticated = computed(() => this._token() !== null);
  readonly token = this._token.asReadonly();

  login(username: string, password: string, remember = false): Observable<boolean> {
    return this.api.login({ username, password }).pipe(
      tap((response) => {
        setAuthToken(response.token, remember);
        this._token.set(response.token);
        this.currentUser.setProfile(this.toCurrentProfile(response.user));
      }),
      map(() => true),
      catchError(() => of(false)),
    );
  }

  invalidateLocalSession(): void {
    clearAuthToken();
    this._token.set(null);
    this.currentUser.resetProfile();
  }

  prepareForSignIn(): void {
    this.invalidateLocalSession();
  }

  logout(): void {
    clearAuthToken();
    this._token.set(null);
    this.currentUser.resetProfile();
    this.router.navigate(['/auth/login']);
  }

  restoreSession(): Observable<void> {
    const token = getAuthToken();
    if (!token) {
      return of(void 0);
    }

    this._token.set(token);
    return this.api.me().pipe(
      tap((user) => this.currentUser.setProfile(this.toCurrentProfile(user))),
      map(() => void 0),
      catchError(() => {
        this.clearSession();
        return of(void 0);
      }),
    );
  }

  updateMyProfile(body: UpdateMyProfileRequest): Observable<boolean> {
    return this.api.updateMyProfile(body).pipe(
      tap((user) => this.currentUser.setProfile(this.toCurrentProfile(user))),
      map(() => true),
      catchError(() => of(false)),
    );
  }

  private clearSession(): void {
    clearAuthToken();
    this._token.set(null);
    this.currentUser.resetProfile();
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
