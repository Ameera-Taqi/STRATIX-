import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { LangSwitcherComponent } from '../../layout/lang-switcher/lang-switcher.component';
import { ThemeToggleComponent } from '../../layout/theme-toggle/theme-toggle.component';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { isStrongPassword } from '../../shared/utils/password.util';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [TranslatePipe, LangSwitcherComponent, ThemeToggleComponent, FormsModule, RouterLink],
  template: `
    <div class="stratix-auth-page">
      <div class="stratix-auth-wash"></div>
      <div class="stratix-auth-grid"></div>
      <div class="stratix-auth-glow"></div>

      <div class="absolute end-6 top-6 z-20 flex gap-2">
        <app-theme-toggle />
        <app-lang-switcher />
      </div>

      <div class="relative z-10 w-full max-w-md">
        <div class="mb-8 flex flex-col items-center text-center">
          <img src="/stratix-logo.svg" alt="STRATIX" class="h-16 w-16 rounded-2xl shadow-xl shadow-primary/20 ring-1 ring-slate-200 dark:ring-white/10" />
          <h1 class="stratix-auth-title">{{ 'auth.resetTitle' | t }}</h1>
          <p class="stratix-auth-subtitle">{{ 'auth.resetHint' | t }}</p>
        </div>

        <div class="stratix-auth-card">
          @if (!token()) {
            <p class="stratix-auth-error mb-0">
              {{ 'auth.errorInvalidToken' | t }}
            </p>
          } @else if (success()) {
            <p class="stratix-auth-success">
              {{ success()! | t }}
            </p>
            <p class="mt-6 text-center text-sm">
              <a routerLink="/auth/login" class="font-medium text-primary hover:underline">{{ 'auth.backToLogin' | t }}</a>
            </p>
          } @else {
            @if (error()) {
              <p class="stratix-auth-error">
                {{ error()! | t }}
              </p>
            }

            <form class="space-y-5" (ngSubmit)="onSubmit()">
              <div>
                <label class="stratix-auth-label">{{ 'auth.newPassword' | t }}</label>
                <div class="relative">
                  <input
                    [type]="showPassword() ? 'text' : 'password'"
                    class="stratix-auth-input pe-10"
                    [placeholder]="'auth.passwordPlaceholder' | t"
                    [(ngModel)]="password"
                    name="password"
                    autocomplete="new-password"
                    required
                  />
                  <button
                    type="button"
                    class="absolute end-2 top-1/2 -translate-y-1/2 rounded-md p-1.5 text-slate-400 transition hover:bg-slate-100 hover:text-dark dark:hover:bg-white/10 dark:hover:text-white"
                    [attr.aria-label]="(showPassword() ? 'module.hidePassword' : 'module.showPassword') | t"
                    (click)="togglePasswordVisibility()"
                  >
                    @if (showPassword()) {
                      <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" aria-hidden="true">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M3 3l18 18M10.5 10.5a3 3 0 004.24 4.24M9.88 5.09A10.94 10.94 0 0112 5c5.52 0 10 4.48 10 10a10.94 10.94 0 01-1.09 4.88M6.23 6.23A10.94 10.94 0 002 12c0 5.52 4.48 10 10 10a10.94 10.94 0 004.77-1.23" />
                      </svg>
                    } @else {
                      <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" aria-hidden="true">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7-10-7-10-7z" />
                        <circle cx="12" cy="12" r="3" stroke-linecap="round" stroke-linejoin="round" />
                      </svg>
                    }
                  </button>
                </div>
                <p class="mt-1.5 text-xs text-slate-500 dark:text-slate-500">{{ 'auth.passwordRules' | t }}</p>
              </div>
              <div>
                <label class="stratix-auth-label">{{ 'auth.confirmPassword' | t }}</label>
                <input
                  [type]="showPassword() ? 'text' : 'password'"
                  class="stratix-auth-input"
                  [placeholder]="'auth.passwordPlaceholder' | t"
                  [(ngModel)]="confirmPassword"
                  name="confirmPassword"
                  autocomplete="new-password"
                  required
                />
              </div>
              <button
                type="submit"
                class="w-full rounded-lg bg-primary py-2.5 text-sm font-semibold text-white shadow-lg shadow-primary/30 transition hover:bg-primary/90 hover:shadow-primary/40 disabled:cursor-not-allowed disabled:opacity-60"
                [disabled]="loading()"
              >
                {{ loading() ? ('common.loading' | t) : ('auth.resetSubmit' | t) }}
              </button>
            </form>
          }

          @if (!success()) {
            <p class="mt-6 text-center text-sm">
              <a routerLink="/auth/login" class="font-medium text-primary hover:underline">{{ 'auth.backToLogin' | t }}</a>
            </p>
          }
        </div>

        <p class="stratix-auth-footer">{{ 'auth.footer' | t }}</p>
      </div>
    </div>
  `,
})
export class ResetPasswordComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);

  readonly token = signal(this.route.snapshot.queryParamMap.get('token') ?? '');
  password = '';
  confirmPassword = '';
  readonly showPassword = signal(false);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly success = signal<string | null>(null);

  togglePasswordVisibility(): void {
    this.showPassword.update((visible) => !visible);
  }

  onSubmit(): void {
    if (!this.token()) {
      this.error.set('auth.errorInvalidToken');
      return;
    }
    if (!isStrongPassword(this.password)) {
      this.error.set('auth.errorWeakPassword');
      return;
    }
    if (this.password !== this.confirmPassword) {
      this.error.set('auth.errorPasswordMismatch');
      return;
    }

    this.error.set(null);
    this.loading.set(true);

    this.api.resetPassword({ token: this.token(), password: this.password }).subscribe({
      next: () => {
        this.loading.set(false);
        this.auth.invalidateLocalSession();
        this.success.set('auth.resetSuccess');
      },
      error: (err) => {
        this.loading.set(false);
        const detail = err?.error?.detail ?? err?.error?.message ?? '';
        if (typeof detail === 'string' && detail.toLowerCase().includes('token')) {
          this.error.set('auth.errorInvalidToken');
        } else {
          this.error.set('auth.errorResetFailed');
        }
      },
    });
  }
}
