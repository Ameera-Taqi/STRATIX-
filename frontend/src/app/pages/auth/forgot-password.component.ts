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
    <div class="relative flex min-h-screen items-center justify-center overflow-hidden bg-slate-950 p-6">
      <div class="pointer-events-none absolute inset-0 bg-gradient-to-b from-slate-900 via-slate-950 to-black"></div>
      <div class="pointer-events-none absolute inset-0 opacity-60" style="background-image: radial-gradient(rgba(148,163,184,0.10) 1px, transparent 1px); background-size: 24px 24px;"></div>
      <div class="pointer-events-none absolute -top-32 left-1/2 h-[28rem] w-[28rem] -translate-x-1/2 rounded-full bg-primary/20 blur-[120px]"></div>

      <div class="absolute end-6 top-6 z-20 flex gap-2">
        <app-theme-toggle />
        <app-lang-switcher />
      </div>

      <div class="relative z-10 w-full max-w-md">
        <div class="mb-8 flex flex-col items-center text-center">
          <img src="/stratix-logo.svg" alt="STRATIX" class="h-16 w-16 rounded-2xl shadow-xl shadow-primary/20 ring-1 ring-white/10" />
          <h1 class="mt-5 text-2xl font-bold tracking-tight text-white">{{ 'auth.forgotTitle' | t }}</h1>
          <p class="mt-1.5 text-sm text-slate-400">{{ 'auth.forgotHint' | t }}</p>
        </div>

        <div class="rounded-2xl border border-white/10 bg-white/[0.03] p-8 shadow-2xl shadow-black/50 backdrop-blur-xl">
          @if (success()) {
            <p class="rounded-lg border border-emerald-500/20 bg-emerald-500/10 px-3 py-2 text-sm text-emerald-300">
              {{ success()! | t }}
            </p>
            <p class="mt-3 rounded-lg border border-white/10 bg-white/[0.03] px-3 py-2 text-xs text-slate-300">
              {{ 'auth.forgotEmailHint' | t }}
            </p>
            @if (devMailInboxUrl) {
              <p class="mt-3 rounded-lg border border-primary/20 bg-primary/10 px-3 py-2 text-xs text-slate-200">
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
            <p class="rounded-lg border border-red-500/20 bg-red-500/10 px-3 py-2 text-sm text-red-300">
              {{ error()! | t }}
            </p>
          }

          @if (!submitted()) {
            <form class="space-y-5" [class.mt-5]="error()" (ngSubmit)="onSubmit()">
              <div>
                <label class="mb-1.5 block text-sm font-medium text-slate-300">{{ 'auth.email' | t }}</label>
                <input
                  type="email"
                  class="w-full rounded-lg border border-white/10 bg-white/5 px-3.5 py-2.5 text-sm text-white placeholder:text-slate-500 outline-none transition focus:border-primary/60 focus:bg-white/[0.07] focus:ring-2 focus:ring-primary/20"
                  placeholder="you@company.com"
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

        <p class="mt-6 text-center text-xs text-slate-500">© STRATIX — Plan · Execute · Achieve</p>
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
