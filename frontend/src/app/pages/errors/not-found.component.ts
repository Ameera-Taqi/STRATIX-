import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../../core/i18n/translate.pipe';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [TranslatePipe, RouterLink],
  template: `
    <div class="flex min-h-screen flex-col items-center justify-center gap-4 bg-slate-50 px-6 text-center dark:bg-slate-950">
      <p class="text-6xl font-bold text-primary">404</p>
      <h1 class="text-2xl font-semibold text-dark dark:text-slate-100">{{ 'errors.notFoundTitle' | t }}</h1>
      <p class="max-w-md text-sm text-slate-600 dark:text-slate-400">{{ 'errors.notFoundHint' | t }}</p>
      <a routerLink="/dashboard" class="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90">
        {{ 'errors.backHome' | t }}
      </a>
    </div>
  `,
})
export class NotFoundComponent {}
