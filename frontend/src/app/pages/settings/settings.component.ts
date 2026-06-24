import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { CurrentUserService } from '../../core/services/current-user.service';
import { AuthService } from '../../core/services/auth.service';
import { EmployeesStore } from '../../core/services/employees.store';
import { employeeInitials } from '../../shared/utils/employee.util';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [TopbarComponent, TranslatePipe, FormsModule, RouterLink],
  template: `
    <app-topbar titleKey="page.settings" />
    <main class="stratix-page p-6">
      <div class="mx-auto max-w-2xl space-y-6">
        <div class="stratix-card overflow-hidden">
          <div class="stratix-profile-hero">
            <div class="flex items-center gap-4">
              <span class="stratix-avatar stratix-avatar-lg ring-4 ring-white dark:ring-slate-800">
                {{ initials(profile().name) }}
              </span>
              <div>
                <h2 class="text-xl font-bold text-dark dark:text-slate-100">{{ profile().name }}</h2>
                <p class="mt-1 text-sm text-slate-600 dark:text-slate-300">{{ profile().role }}</p>
                <span class="stratix-badge mt-2 bg-primary/10 text-primary">{{ profile().department }}</span>
              </div>
            </div>
          </div>

          <form class="space-y-4 p-6" (ngSubmit)="saveProfile()">
            <div>
              <label class="stratix-muted mb-1.5 block text-sm font-medium">{{ 'common.name' | t }}</label>
              <input type="text" class="stratix-input w-full px-3 py-2.5 text-sm" [(ngModel)]="form.name" name="name" required />
            </div>
            <div>
              <label class="stratix-muted mb-1.5 block text-sm font-medium">{{ 'settings.email' | t }}</label>
              <input type="email" class="stratix-input w-full px-3 py-2.5 text-sm" [(ngModel)]="form.email" name="email" required />
            </div>
            <div>
              <label class="stratix-muted mb-1.5 block text-sm font-medium">{{ 'common.department' | t }}</label>
              <select class="stratix-input w-full px-3 py-2.5 text-sm" [(ngModel)]="form.department" name="department" required>
                @for (dept of departments(); track dept) {
                  <option [value]="dept">{{ dept }}</option>
                }
              </select>
            </div>
            <div>
              <label class="stratix-muted mb-1.5 block text-sm font-medium">{{ 'team.role' | t }}</label>
              <input type="text" class="stratix-input w-full px-3 py-2.5 text-sm bg-slate-50 dark:bg-slate-800/60" [value]="profile().role" readonly />
            </div>

            @if (error()) {
              <p class="rounded-lg bg-red-50 px-3 py-2 text-sm text-danger dark:bg-red-900/30 dark:text-red-300">
                {{ error()! | t }}
              </p>
            } @else if (saved()) {
              <p class="rounded-lg bg-emerald-50 px-3 py-2 text-sm text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-300">
                {{ 'settings.saved' | t }}
              </p>
            }

            <div class="flex flex-wrap items-center justify-between gap-3 border-t border-slate-100 pt-4 dark:border-slate-700">
              <a
                [routerLink]="['/team', profile().employeeId]"
                class="text-sm font-medium text-primary hover:underline"
              >
                {{ 'settings.viewTeamProfile' | t }} →
              </a>
              <button
                type="submit"
                class="rounded-lg bg-primary px-5 py-2 text-sm font-medium text-white shadow-sm hover:bg-primary/90 disabled:cursor-not-allowed disabled:opacity-60"
                [disabled]="saving()"
              >
                @if (saving()) {
                  {{ 'common.loading' | t }}
                } @else {
                  {{ 'settings.saveProfile' | t }}
                }
              </button>
            </div>
          </form>
        </div>

        <div class="stratix-card p-6">
          <h3 class="stratix-heading">{{ 'settings.title' | t }}</h3>
          <p class="mt-2 text-sm text-slate-500">{{ 'settings.hint' | t }}</p>
        </div>
      </div>
    </main>
  `,
})
export class SettingsComponent implements OnInit {
  private readonly currentUser = inject(CurrentUserService);
  private readonly auth = inject(AuthService);
  private readonly employeesStore = inject(EmployeesStore);

  readonly profile = this.currentUser.profile;
  readonly departments = this.employeesStore.departmentOptions;
  readonly initials = employeeInitials;

  form = {
    name: '',
    email: '',
    department: '',
  };

  readonly saving = signal(false);
  readonly saved = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    if (!this.employeesStore.loaded()) {
      this.employeesStore.loadFromApi();
    }
    this.syncFormFromProfile();
  }

  saveProfile(): void {
    this.error.set(null);
    this.saved.set(false);
    this.saving.set(true);

    this.auth
      .updateMyProfile({
        name: this.form.name.trim(),
        email: this.form.email.trim().toLowerCase(),
        department: this.form.department.trim(),
      })
      .subscribe((ok) => {
        this.saving.set(false);
        if (!ok) {
          this.error.set('settings.saveFailed');
          return;
        }

        this.syncFormFromProfile();
        this.employeesStore.refreshUser(this.profile().id).subscribe();
        this.saved.set(true);
        window.setTimeout(() => this.saved.set(false), 2500);
      });
  }

  private syncFormFromProfile(): void {
    const p = this.profile();
    this.form = {
      name: p.name,
      email: p.email,
      department: p.department,
    };
  }
}
