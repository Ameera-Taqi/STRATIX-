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
    <div class="flex min-h-screen flex-col items-center justify-center stratix-page p-6">
      <div class="absolute end-6 top-6 flex gap-2">
        <app-theme-toggle />
        <app-lang-switcher />
      </div>
      <img src="/stratix-logo.png" alt="STRATIX" class="mb-6 h-32 w-auto" />
      <div class="stratix-card w-full max-w-md overflow-hidden p-8">
        <div class="mb-1 flex h-11 w-11 items-center justify-center rounded-xl bg-primary/10 text-xl">✉️</div>
        <h1 class="stratix-heading mt-3 text-xl">{{ 'auth.forgotTitle' | t }}</h1>
        <p class="stratix-muted mt-2 text-sm">{{ 'auth.forgotHint' | t }}</p>

        @if (success()) {
          <p class="mt-4 rounded-lg bg-emerald-50 px-3 py-2 text-sm text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-200">
            {{ success()! | t }}
          </p>
          <p class="mt-3 rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-xs text-slate-600 dark:border-slate-600 dark:bg-slate-800/60 dark:text-slate-300">
            {{ 'auth.forgotEmailHint' | t }}
          </p>
          @if (devMailInboxUrl) {
            <p class="mt-3 rounded-lg border border-primary/20 bg-primary/5 px-3 py-2 text-xs text-slate-700 dark:text-slate-200">
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
          <p class="mt-4 rounded-lg bg-red-50 px-3 py-2 text-sm text-danger dark:bg-red-900/30 dark:text-red-300">
            {{ error()! | t }}
          </p>
        }

        @if (!submitted()) {
          <form class="mt-6 space-y-4" (ngSubmit)="onSubmit()">
            <div>
              <label class="stratix-muted mb-1 block text-sm">{{ 'auth.email' | t }}</label>
              <input
                type="email"
                class="stratix-input w-full px-3 py-2 text-sm"
                [(ngModel)]="email"
                name="email"
                autocomplete="email"
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
                {{ 'auth.sendResetLink' | t }}
              }
            </button>
          </form>
        }

        <p class="mt-5 text-center text-sm">
          <a routerLink="/auth/login" class="font-medium text-primary hover:underline">{{ 'auth.backToLogin' | t }}</a>
        </p>
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
