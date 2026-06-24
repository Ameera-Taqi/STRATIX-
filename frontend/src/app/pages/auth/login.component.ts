import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { LangSwitcherComponent } from '../../layout/lang-switcher/lang-switcher.component';
import { ThemeToggleComponent } from '../../layout/theme-toggle/theme-toggle.component';
import { AuthService } from '../../core/services/auth.service';
import { isRememberMeEnabled } from '../../core/services/auth-token.storage';
import { DataBootstrapService } from '../../core/services/data-bootstrap.service';

@Component({
  selector: 'app-login',
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
        <div class="mb-1 flex h-11 w-11 items-center justify-center rounded-xl bg-primary/10 text-xl">🔐</div>
        <h1 class="stratix-heading mt-3 text-xl">{{ 'module.auth' | t }}</h1>
        <p class="stratix-muted mt-2 text-sm">{{ 'module.authHint' | t }}</p>

        @if (error()) {
          <p class="mt-4 rounded-lg bg-red-50 px-3 py-2 text-sm text-danger dark:bg-red-900/30 dark:text-red-300">
            {{ error()! | t }}
          </p>
        }

        <form class="mt-6 space-y-4" (ngSubmit)="onSubmit()">
          <div>
            <label class="stratix-muted mb-1 block text-sm">{{ 'module.username' | t }}</label>
            <input
              type="text"
              class="stratix-input w-full px-3 py-2 text-sm"
              [(ngModel)]="username"
              name="username"
              autocomplete="username"
              required
            />
          </div>
          <div>
            <label class="stratix-muted mb-1 block text-sm">{{ 'module.password' | t }}</label>
            <div class="relative">
              <input
                [type]="showPassword() ? 'text' : 'password'"
                class="stratix-input w-full px-3 py-2 pe-10 text-sm"
                [(ngModel)]="password"
                name="password"
                autocomplete="current-password"
                required
              />
              <button
                type="button"
                class="absolute end-2 top-1/2 -translate-y-1/2 rounded-md p-1.5 text-slate-500 transition hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-700 dark:hover:text-slate-200"
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
            <div class="text-end">
              <a routerLink="/auth/forgot-password" class="text-xs font-medium text-primary hover:underline">
                {{ 'auth.forgotLink' | t }}
              </a>
            </div>
          </div>

          <label class="flex cursor-pointer items-center gap-2 text-sm text-slate-600 dark:text-slate-300">
            <input
              type="checkbox"
              class="h-4 w-4 rounded border-slate-300 text-primary focus:ring-primary/30"
              [(ngModel)]="rememberMe"
              name="rememberMe"
            />
            <span>{{ 'module.rememberMe' | t }}</span>
          </label>

          <button
            type="submit"
            class="w-full rounded-lg bg-primary py-2.5 text-sm font-medium text-white transition hover:bg-primary/90 disabled:cursor-not-allowed disabled:opacity-60"
            [disabled]="submitting()"
          >
            {{ 'module.signIn' | t }}
          </button>
        </form>
      </div>
    </div>
  `,
})
export class LoginComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly bootstrap = inject(DataBootstrapService);

  username = '';
  password = '';
  rememberMe = isRememberMeEnabled();
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
    this.error.set(null);
    this.submitting.set(true);

    this.auth.login(this.username, this.password, this.rememberMe).subscribe((ok) => {
      this.submitting.set(false);

      if (!ok) {
        this.error.set('module.invalidCredentials');
        return;
      }

      this.bootstrap.bootstrap();
      this.router.navigate(['/dashboard']);
    });
  }
}
