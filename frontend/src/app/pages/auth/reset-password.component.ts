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
    <div class="flex min-h-screen flex-col items-center justify-center stratix-page p-6">
      <div class="absolute end-6 top-6 flex gap-2">
        <app-theme-toggle />
        <app-lang-switcher />
      </div>
      <img src="/stratix-logo.png" alt="STRATIX" class="mb-6 h-32 w-auto" />
      <div class="stratix-card w-full max-w-md overflow-hidden p-8">
        <div class="mb-1 flex h-11 w-11 items-center justify-center rounded-xl bg-primary/10 text-xl">🔑</div>
        <h1 class="stratix-heading mt-3 text-xl">{{ 'auth.resetTitle' | t }}</h1>
        <p class="stratix-muted mt-2 text-sm">{{ 'auth.resetHint' | t }}</p>

        @if (!token()) {
          <p class="mt-4 rounded-lg bg-red-50 px-3 py-2 text-sm text-danger dark:bg-red-900/30 dark:text-red-300">
            {{ 'auth.errorInvalidToken' | t }}
          </p>
        } @else if (success()) {
          <p class="mt-4 rounded-lg bg-emerald-50 px-3 py-2 text-sm text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-200">
            {{ success()! | t }}
          </p>
          <p class="mt-5 text-center text-sm">
            <a routerLink="/auth/login" class="font-medium text-primary hover:underline">{{ 'auth.backToLogin' | t }}</a>
          </p>
        } @else {
          @if (error()) {
            <p class="mt-4 rounded-lg bg-red-50 px-3 py-2 text-sm text-danger dark:bg-red-900/30 dark:text-red-300">
              {{ error()! | t }}
            </p>
          }

          <form class="mt-6 space-y-4" (ngSubmit)="onSubmit()">
            <div>
              <label class="stratix-muted mb-1 block text-sm">{{ 'auth.newPassword' | t }}</label>
              <div class="relative">
                <input
                  [type]="showPassword() ? 'text' : 'password'"
                  class="stratix-input w-full px-3 py-2 pe-10 text-sm"
                  [(ngModel)]="password"
                  name="password"
                  autocomplete="new-password"
                  required
                />
                <button
                  type="button"
                  class="absolute end-2 top-1/2 -translate-y-1/2 rounded-md p-1.5 text-slate-500"
                  [attr.aria-label]="(showPassword() ? 'module.hidePassword' : 'module.showPassword') | t"
                  (click)="togglePasswordVisibility()"
                >
                  {{ showPassword() ? '🙈' : '👁' }}
                </button>
              </div>
              <p class="mt-1 text-xs stratix-muted">{{ 'auth.passwordRules' | t }}</p>
            </div>
            <div>
              <label class="stratix-muted mb-1 block text-sm">{{ 'auth.confirmPassword' | t }}</label>
              <input
                [type]="showPassword() ? 'text' : 'password'"
                class="stratix-input w-full px-3 py-2 text-sm"
                [(ngModel)]="confirmPassword"
                name="confirmPassword"
                autocomplete="new-password"
                required
              />
            </div>
            <button
              type="submit"
              class="w-full rounded-lg bg-primary py-2.5 text-sm font-medium text-white transition hover:bg-primary/90 disabled:cursor-not-allowed disabled:opacity-60"
              [disabled]="loading()"
            >
              @if (loading()) {
                {{ 'common.loading' | t }}
              } @else {
                {{ 'auth.resetSubmit' | t }}
              }
            </button>
          </form>
        }

        @if (!success()) {
          <p class="mt-5 text-center text-sm">
            <a routerLink="/auth/login" class="font-medium text-primary hover:underline">{{ 'auth.backToLogin' | t }}</a>
          </p>
        }
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
