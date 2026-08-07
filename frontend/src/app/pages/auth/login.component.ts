import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { LangSwitcherComponent } from '../../layout/lang-switcher/lang-switcher.component';
import { ThemeToggleComponent } from '../../layout/theme-toggle/theme-toggle.component';
import { AuthService } from '../../core/services/auth.service';
import { isRememberMeEnabled } from '../../core/services/auth-token.storage';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [TranslatePipe, LangSwitcherComponent, ThemeToggleComponent, ReactiveFormsModule, RouterLink],
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
          <img
            src="/stratix-logo.svg"
            alt="STRATIX"
            class="h-16 w-16 rounded-2xl shadow-xl shadow-primary/20 ring-1 ring-slate-200 dark:ring-white/10"
          />
          <h1 class="stratix-auth-title">{{ 'auth.welcome' | t }}</h1>
          <p class="stratix-auth-subtitle">{{ 'auth.welcomeHint' | t }}</p>
        </div>

        <div class="stratix-auth-card">
          @if (error()) {
            <p class="stratix-auth-error" role="alert">{{ error()! | t }}</p>
          }

          <form class="space-y-5" [formGroup]="form" (ngSubmit)="onSubmit()">
            <div>
              <label class="stratix-auth-label" for="login-username">{{ 'module.username' | t }}</label>
              <input
                id="login-username"
                type="text"
                class="stratix-auth-input"
                [placeholder]="'auth.emailPlaceholder' | t"
                formControlName="username"
                autocomplete="username"
              />
            </div>
            <div>
              <label class="stratix-auth-label" for="login-password">{{ 'module.password' | t }}</label>
              <div class="relative">
                <input
                  id="login-password"
                  [type]="showPassword() ? 'text' : 'password'"
                  class="stratix-auth-input pe-10"
                  [placeholder]="'auth.passwordPlaceholder' | t"
                  formControlName="password"
                  autocomplete="current-password"
                />
                <button
                  type="button"
                  class="absolute end-2 top-1/2 -translate-y-1/2 rounded-md p-1.5 text-slate-400 transition hover:bg-slate-100 hover:text-dark dark:hover:bg-white/10 dark:hover:text-white"
                  [attr.aria-label]="(showPassword() ? 'module.hidePassword' : 'module.showPassword') | t"
                  [attr.aria-pressed]="showPassword()"
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
              <div class="mt-1.5 text-end">
                <a routerLink="/auth/forgot-password" class="text-xs font-medium text-primary hover:underline">
                  {{ 'auth.forgotLink' | t }}
                </a>
              </div>
            </div>

            <label class="flex cursor-pointer items-center gap-2 stratix-auth-muted">
              <input
                type="checkbox"
                class="h-4 w-4 rounded border-slate-300 bg-white text-primary focus:ring-primary/30 dark:border-white/20 dark:bg-white/5"
                formControlName="rememberMe"
              />
              <span>{{ 'module.rememberMe' | t }}</span>
            </label>

            <button
              type="submit"
              class="w-full rounded-lg bg-primary py-2.5 text-sm font-semibold text-white shadow-lg shadow-primary/30 transition hover:bg-primary/90 hover:shadow-primary/40 disabled:cursor-not-allowed disabled:opacity-60"
              [disabled]="submitting() || form.invalid"
            >
              {{ submitting() ? ('common.loading' | t) : ('module.signIn' | t) }}
            </button>
          </form>

          <p class="mt-6 text-center stratix-auth-muted">
            {{ 'auth.noAccount' | t }}
            <a routerLink="/auth/register" class="ms-1 font-medium text-primary hover:underline">{{ 'auth.signUpLink' | t }}</a>
          </p>
        </div>

        <p class="stratix-auth-footer">{{ 'auth.footer' | t }}</p>
      </div>
    </div>
  `,
})
export class LoginComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.nonNullable.group({
    username: ['', [Validators.required]],
    password: ['', [Validators.required]],
    rememberMe: [isRememberMeEnabled()],
  });

  readonly showPassword = signal(false);
  readonly error = signal<string | null>(null);
  readonly submitting = signal(false);

  ngOnInit(): void {
    this.auth.prepareForSignIn();
  }

  togglePasswordVisibility(): void {
    this.showPassword.update((visible) => !visible);
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.error.set(null);
    this.submitting.set(true);
    const { username, password, rememberMe } = this.form.getRawValue();

    this.auth.login(username, password, rememberMe).subscribe((ok) => {
      this.submitting.set(false);
      if (!ok) {
        this.error.set('module.invalidCredentials');
        return;
      }
      void this.router.navigate(['/dashboard']);
    });
  }
}
