import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { LangSwitcherComponent } from '../../layout/lang-switcher/lang-switcher.component';
import { ThemeToggleComponent } from '../../layout/theme-toggle/theme-toggle.component';
import { ApiService } from '../../core/services/api.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-forgot-password',
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
          <h1 class="stratix-auth-title">{{ 'auth.forgotTitle' | t }}</h1>
          <p class="stratix-auth-subtitle">{{ 'auth.forgotHint' | t }}</p>
        </div>

        <div class="stratix-auth-card">
          @if (success()) {
            <p class="stratix-auth-success">
              {{ success()! | t }}
            </p>
            <p class="mt-3 rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-xs text-slate-600 dark:border-white/10 dark:bg-white/[0.03] dark:text-slate-300">
              {{ 'auth.forgotEmailHint' | t }}
            </p>
            @if (devMailInboxUrl) {
              <p class="mt-3 rounded-lg border border-primary/20 bg-primary/10 px-3 py-2 text-xs text-slate-700 dark:text-slate-200">
                {{ 'auth.forgotDevMailHint' | t }}
                <a
                  [href]="devMailInboxUrl"
                  target="_blank"
                  rel="noopener noreferrer"
                  class="ms-1 font-medium text-primary hover:underline"
                >
                  {{ devMailInboxUrl }}
                </a>
              </p>
            }
          } @else if (error()) {
            <p class="stratix-auth-error mb-0">
              {{ error()! | t }}
            </p>
          }

          @if (!submitted()) {
            <form class="space-y-5" [class.mt-5]="error()" (ngSubmit)="onSubmit()">
              <div>
                <label class="stratix-auth-label">{{ 'auth.email' | t }}</label>
                <input
                  type="email"
                  class="stratix-auth-input"
                  [placeholder]="'auth.emailPlaceholder' | t"
                  [(ngModel)]="email"
                  name="email"
                  autocomplete="email"
                  required
                />
              </div>
              <button
                type="submit"
                class="w-full rounded-lg bg-primary py-2.5 text-sm font-semibold text-white shadow-lg shadow-primary/30 transition hover:bg-primary/90 hover:shadow-primary/40 disabled:cursor-not-allowed disabled:opacity-60"
                [disabled]="loading()"
              >
                {{ loading() ? ('common.loading' | t) : ('auth.sendResetLink' | t) }}
              </button>
            </form>
          }

          <p class="mt-6 text-center text-sm">
            <a routerLink="/auth/login" class="font-medium text-primary hover:underline">{{ 'auth.backToLogin' | t }}</a>
          </p>
        </div>

        <p class="stratix-auth-footer">{{ 'auth.footer' | t }}</p>
      </div>
    </div>
  `,
})
export class ForgotPasswordComponent {
  private readonly api = inject(ApiService);

  readonly devMailInboxUrl = environment.production ? null : environment.mailDevInboxUrl;

  email = '';
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly success = signal<string | null>(null);
  readonly submitted = signal(false);

  onSubmit(): void {
    if (!this.email.trim()) {
      this.error.set('auth.errorEmailRequired');
      return;
    }

    this.error.set(null);
    this.loading.set(true);

    this.api.forgotPassword({ email: this.email.trim() }).subscribe({
      next: () => {
        this.loading.set(false);
        this.submitted.set(true);
        this.success.set('auth.forgotSuccess');
      },
      error: () => {
        this.loading.set(false);
        this.submitted.set(true);
        this.success.set('auth.forgotSuccess');
      },
    });
  }
}
