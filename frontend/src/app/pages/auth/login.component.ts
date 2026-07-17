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
    <div class="relative flex min-h-screen items-center justify-center overflow-hidden bg-slate-950 p-6">
      <!-- ambient background: gradient wash, dotted grid, and a soft blue glow -->
      <div class="pointer-events-none absolute inset-0 bg-gradient-to-b from-slate-900 via-slate-950 to-black"></div>
      <div class="pointer-events-none absolute inset-0 opacity-60" style="background-image: radial-gradient(rgba(148,163,184,0.10) 1px, transparent 1px); background-size: 24px 24px;"></div>
      <div class="pointer-events-none absolute -top-32 left-1/2 h-[28rem] w-[28rem] -translate-x-1/2 rounded-full bg-primary/20 blur-[120px]"></div>

      <div class="absolute end-6 top-6 z-20 flex gap-2">
        <app-theme-toggle />
        <app-lang-switcher />
      </div>

      <div class="relative z-10 w-full max-w-md">
        <!-- brand lockup -->
        <div class="mb-8 flex flex-col items-center text-center">
          <img src="/stratix-logo.svg" alt="STRATIX" class="h-16 w-16 rounded-2xl shadow-xl shadow-primary/20 ring-1 ring-white/10" />
          <h1 class="mt-5 text-2xl font-bold tracking-tight text-white">{{ 'auth.welcome' | t }}</h1>
          <p class="mt-1.5 text-sm text-slate-400">{{ 'auth.welcomeHint' | t }}</p>
        </div>

        <!-- glass card -->
        <div class="rounded-2xl border border-white/10 bg-white/[0.03] p-8 shadow-2xl shadow-black/50 backdrop-blur-xl">
          @if (error()) {
            <p class="mb-5 rounded-lg border border-red-500/20 bg-red-500/10 px-3 py-2 text-sm text-red-300">
              {{ error()! | t }}
            </p>
          }

          <form class="space-y-5" (ngSubmit)="onSubmit()">
            <div>
              <label class="mb-1.5 block text-sm font-medium text-slate-300">{{ 'module.username' | t }}</label>
              <input
                type="text"
                class="w-full rounded-lg border border-white/10 bg-white/5 px-3.5 py-2.5 text-sm text-white placeholder:text-slate-500 outline-none transition focus:border-primary/60 focus:bg-white/[0.07] focus:ring-2 focus:ring-primary/20"
                placeholder="you@company.com"
                [(ngModel)]="username"
                name="username"
                autocomplete="username"
                required
              />
            </div>
            <div>
              <label class="mb-1.5 block text-sm font-medium text-slate-300">{{ 'module.password' | t }}</label>
              <div class="relative">
                <input
                  [type]="showPassword() ? 'text' : 'password'"
                  class="w-full rounded-lg border border-white/10 bg-white/5 px-3.5 py-2.5 pe-10 text-sm text-white placeholder:text-slate-500 outline-none transition focus:border-primary/60 focus:bg-white/[0.07] focus:ring-2 focus:ring-primary/20"
                  placeholder="••••••••"
                  [(ngModel)]="password"
                  name="password"
                  autocomplete="current-password"
                  required
                />
                <button
                  type="button"
                  class="absolute end-2 top-1/2 -translate-y-1/2 rounded-md p-1.5 text-slate-400 transition hover:bg-white/10 hover:text-white"
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

            <label class="flex cursor-pointer items-center gap-2 text-sm text-slate-400">
              <input
                type="checkbox"
                class="h-4 w-4 rounded border-white/20 bg-white/5 text-primary focus:ring-primary/30"
                [(ngModel)]="rememberMe"
                name="rememberMe"
              />
              <span>{{ 'module.rememberMe' | t }}</span>
            </label>

            <button
              type="submit"
              class="w-full rounded-lg bg-primary py-2.5 text-sm font-semibold text-white shadow-lg shadow-primary/30 transition hover:bg-primary/90 hover:shadow-primary/40 disabled:cursor-not-allowed disabled:opacity-60"
              [disabled]="submitting()"
            >
              {{ submitting() ? ('common.loading' | t) : ('module.signIn' | t) }}
            </button>
          </form>

          <p class="mt-6 text-center text-sm text-slate-400">
            {{ 'auth.noAccount' | t }}
            <a routerLink="/auth/register" class="ms-1 font-medium text-primary hover:underline">{{ 'auth.signUpLink' | t }}</a>
          </p>
        </div>

        <p class="mt-6 text-center text-xs text-slate-500">© STRATIX — Plan · Execute · Achieve</p>
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
