import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { LanguageService } from '../../core/i18n/language.service';
import { Lang } from '../../core/i18n/translations';
import { LangSwitcherComponent } from '../../layout/lang-switcher/lang-switcher.component';
import { ThemeToggleComponent } from '../../layout/theme-toggle/theme-toggle.component';
import { OnboardingService } from '../../core/services/onboarding.service';
import { BrandingStore } from '../../core/services/branding.store';
import { DepartmentsStore } from '../../core/services/departments.store';
import { EmployeesStore } from '../../core/services/employees.store';
import { ProjectsStore } from '../../core/services/projects.store';
import { CurrentUserService } from '../../core/services/current-user.service';
import { UserRole } from '../../core/models/user.model';
import { UiIconComponent } from '../../shared/components/ui-icon/ui-icon.component';

const INDUSTRIES = [
  'Technology',
  'Healthcare',
  'Finance',
  'Education',
  'Construction',
  'Retail',
  'Manufacturing',
  'Government',
  'Consulting',
  'Other',
] as const;

const TIMEZONES = [
  'Asia/Riyadh',
  'Asia/Dubai',
  'Asia/Kuwait',
  'Asia/Bahrain',
  'Asia/Qatar',
  'Africa/Cairo',
  'Europe/London',
  'Europe/Paris',
  'America/New_York',
  'UTC',
] as const;

@Component({
  selector: 'app-onboarding',
  standalone: true,
  imports: [FormsModule, TranslatePipe, LangSwitcherComponent, ThemeToggleComponent, UiIconComponent],
  template: `
    <div class="stratix-auth-page">
      <div class="stratix-auth-wash"></div>
      <div class="stratix-auth-grid"></div>
      <div class="stratix-auth-glow"></div>

      <div class="absolute end-6 top-6 z-20 flex gap-2">
        <app-theme-toggle />
        <app-lang-switcher />
      </div>

      <div class="relative z-10 w-full max-w-xl px-4 py-10">
        <div class="mb-6 flex flex-col items-center text-center">
          <img
            src="/stratix-logo.svg"
            alt="STRATIX"
            class="h-14 w-14 rounded-2xl shadow-xl shadow-primary/20 ring-1 ring-slate-200 dark:ring-white/10"
          />
          @if (phase() === 'ready') {
            <h1 class="stratix-auth-title mt-4">{{ 'onboarding.readyTitle' | t }}</h1>
            <p class="stratix-auth-subtitle">{{ 'onboarding.readyHint' | t }}</p>
          } @else {
            <h1 class="stratix-auth-title mt-4">{{ 'onboarding.welcome' | t }}</h1>
            <p class="stratix-auth-subtitle">{{ 'onboarding.welcomeHint' | t }}</p>
          }
        </div>

        @if (phase() === 'wizard') {
          <div class="mb-4 flex items-center justify-between gap-3 text-xs font-medium text-slate-500 dark:text-slate-400">
            <span>{{ 'onboarding.stepOf' | t }} {{ step() }} / 4</span>
            <div class="flex flex-1 gap-1.5 px-3">
              @for (n of [1, 2, 3, 4]; track n) {
                <div
                  class="h-1.5 flex-1 rounded-full transition"
                  [class]="n <= step() ? 'bg-primary' : 'bg-slate-200 dark:bg-white/10'"
                ></div>
              }
            </div>
          </div>
        }

        <div class="stratix-auth-card !max-w-none">
          @if (error()) {
            <p class="stratix-auth-error mb-4">{{ error()! | t }}</p>
          }

          @if (phase() === 'ready') {
            <ul class="space-y-3">
              @for (item of checklist(); track item.key) {
                <li
                  class="flex items-center gap-3 rounded-xl border px-4 py-3 text-sm"
                  [class]="
                    item.done
                      ? 'border-emerald-200 bg-emerald-50/80 text-emerald-900 dark:border-emerald-900/50 dark:bg-emerald-950/30 dark:text-emerald-200'
                      : 'border-slate-200 bg-slate-50 text-slate-500 dark:border-white/10 dark:bg-slate-800/40 dark:text-slate-400'
                  "
                >
                  <span
                    class="flex h-7 w-7 shrink-0 items-center justify-center rounded-full"
                    [class]="
                      item.done
                        ? 'bg-emerald-500 text-white'
                        : 'bg-slate-200 text-slate-500 dark:bg-slate-700 dark:text-slate-400'
                    "
                  >
                    @if (item.done) {
                      <app-ui-icon name="check" size="sm" />
                    } @else {
                      <span class="text-xs font-semibold">—</span>
                    }
                  </span>
                  <span class="min-w-0 flex-1 font-medium">{{ item.key | t }}</span>
                  @if (!item.done) {
                    <span class="text-xs font-medium opacity-80">{{ 'onboarding.checkSkipped' | t }}</span>
                  }
                </li>
              }
            </ul>

            <div class="mt-8 flex justify-end">
              <button
                type="button"
                class="rounded-xl bg-primary px-5 py-2.5 text-sm font-semibold text-white shadow-lg shadow-primary/25 transition hover:brightness-110 disabled:opacity-60"
                [disabled]="busy()"
                (click)="goToDashboard()"
              >
                {{ 'onboarding.goDashboard' | t }}
              </button>
            </div>
          } @else {
            @switch (step()) {
              @case (1) {
                <h2 class="mb-1 text-lg font-semibold text-dark dark:text-white">
                  {{ 'onboarding.step1Title' | t }}
                </h2>
                <p class="mb-5 text-sm text-slate-500 dark:text-slate-400">
                  {{ 'onboarding.step1Hint' | t }}
                </p>

                <div class="space-y-4">
                  <div>
                    <label class="stratix-auth-label">{{ 'auth.orgName' | t }}</label>
                    <input class="stratix-auth-input" [(ngModel)]="orgName" name="orgName" />
                  </div>
                  <div>
                    <label class="stratix-auth-label">{{ 'onboarding.logo' | t }}</label>
                    <input
                      type="file"
                      accept="image/png,image/jpeg,image/webp,image/svg+xml"
                      class="block w-full text-sm text-slate-500 file:me-3 file:rounded-lg file:border-0 file:bg-primary/10 file:px-3 file:py-2 file:text-sm file:font-medium file:text-primary"
                      (change)="onLogoSelected($event)"
                    />
                    @if (branding.logoObjectUrl()) {
                      <img
                        [src]="branding.logoObjectUrl()!"
                        alt=""
                        class="mt-3 h-14 w-14 rounded-xl object-cover ring-1 ring-slate-200 dark:ring-white/10"
                      />
                    }
                  </div>
                  <div>
                    <label class="stratix-auth-label">{{ 'onboarding.industry' | t }}</label>
                    <select class="stratix-auth-input" [(ngModel)]="industry" name="industry">
                      <option value="">{{ 'onboarding.selectOptional' | t }}</option>
                      @for (item of industries; track item) {
                        <option [value]="item">{{ item }}</option>
                      }
                    </select>
                  </div>
                  <div>
                    <label class="stratix-auth-label">{{ 'onboarding.timezone' | t }}</label>
                    <select class="stratix-auth-input" [(ngModel)]="timezone" name="timezone">
                      <option value="">{{ 'onboarding.selectOptional' | t }}</option>
                      @for (tz of timezones; track tz) {
                        <option [value]="tz">{{ tz }}</option>
                      }
                    </select>
                  </div>
                  <div>
                    <label class="stratix-auth-label">{{ 'onboarding.language' | t }}</label>
                    <select class="stratix-auth-input" [(ngModel)]="preferredLanguage" name="lang">
                      <option value="en">English</option>
                      <option value="ar">العربية</option>
                    </select>
                  </div>
                </div>
              }
              @case (2) {
                <h2 class="mb-1 text-lg font-semibold text-dark dark:text-white">
                  {{ 'onboarding.step2Title' | t }}
                </h2>
                <p class="mb-5 text-sm text-slate-500 dark:text-slate-400">
                  {{ 'onboarding.step2Hint' | t }}
                </p>
                <div class="space-y-4">
                  <div>
                    <label class="stratix-auth-label">{{ 'onboarding.departmentName' | t }}</label>
                    <input
                      class="stratix-auth-input"
                      [(ngModel)]="departmentName"
                      name="departmentName"
                      [placeholder]="'onboarding.departmentPlaceholder' | t"
                    />
                  </div>
                  <div>
                    <label class="stratix-auth-label">{{ 'onboarding.departmentDesc' | t }}</label>
                    <textarea
                      class="stratix-auth-input min-h-[88px]"
                      [(ngModel)]="departmentDescription"
                      name="departmentDescription"
                    ></textarea>
                  </div>
                </div>
              }
              @case (3) {
                <h2 class="mb-1 text-lg font-semibold text-dark dark:text-white">
                  {{ 'onboarding.step3Title' | t }}
                </h2>
                <p class="mb-5 text-sm text-slate-500 dark:text-slate-400">
                  {{ 'onboarding.step3Hint' | t }}
                </p>
                <div class="space-y-4">
                  <div>
                    <label class="stratix-auth-label">{{ 'onboarding.memberName' | t }}</label>
                    <input class="stratix-auth-input" [(ngModel)]="memberName" name="memberName" />
                  </div>
                  <div>
                    <label class="stratix-auth-label">{{ 'auth.email' | t }}</label>
                    <input
                      type="email"
                      class="stratix-auth-input"
                      [(ngModel)]="memberEmail"
                      name="memberEmail"
                    />
                  </div>
                  <div>
                    <label class="stratix-auth-label">{{ 'onboarding.memberRole' | t }}</label>
                    <select class="stratix-auth-input" [(ngModel)]="memberRole" name="memberRole">
                      <option value="EMPLOYEE">{{ 'role.employee' | t }}</option>
                      <option value="PROJECT_MANAGER">{{ 'role.projectManager' | t }}</option>
                      <option value="TEAM_LEADER">{{ 'role.teamLeader' | t }}</option>
                    </select>
                  </div>
                  <p class="rounded-lg bg-slate-50 px-3 py-2 text-xs text-slate-500 dark:bg-slate-800/60 dark:text-slate-400">
                    {{ 'onboarding.tempPasswordHint' | t }}
                  </p>
                </div>
              }
              @case (4) {
                <h2 class="mb-1 text-lg font-semibold text-dark dark:text-white">
                  {{ 'onboarding.step4Title' | t }}
                </h2>
                <p class="mb-5 text-sm text-slate-500 dark:text-slate-400">
                  {{ 'onboarding.step4Hint' | t }}
                </p>
                <div class="space-y-4">
                  <div>
                    <label class="stratix-auth-label">{{ 'onboarding.projectName' | t }}</label>
                    <input
                      class="stratix-auth-input"
                      [(ngModel)]="projectName"
                      name="projectName"
                      [placeholder]="'onboarding.projectPlaceholder' | t"
                    />
                  </div>
                  @if (departments.departments().length > 0) {
                    <div>
                      <label class="stratix-auth-label">{{ 'onboarding.projectDepartment' | t }}</label>
                      <select class="stratix-auth-input" [(ngModel)]="projectDepartment" name="projectDept">
                        <option value="">{{ 'onboarding.selectOptional' | t }}</option>
                        @for (d of departments.departments(); track d.id) {
                          <option [value]="d.name">{{ d.name }}</option>
                        }
                      </select>
                    </div>
                  }
                </div>
              }
            }

            <div class="mt-8 flex flex-wrap items-center justify-between gap-3">
              <button
                type="button"
                class="text-sm font-medium text-slate-500 transition hover:text-dark dark:hover:text-white"
                [disabled]="busy()"
                (click)="skip()"
              >
                {{ 'onboarding.skip' | t }}
              </button>
              <div class="flex gap-2">
                @if (step() > 1) {
                  <button
                    type="button"
                    class="rounded-xl border border-slate-200 px-4 py-2.5 text-sm font-medium text-dark transition hover:bg-slate-50 dark:border-white/10 dark:text-white dark:hover:bg-white/5"
                    [disabled]="busy()"
                    (click)="back()"
                  >
                    {{ 'onboarding.back' | t }}
                  </button>
                }
                <button
                  type="button"
                  class="rounded-xl bg-primary px-5 py-2.5 text-sm font-semibold text-white shadow-lg shadow-primary/25 transition hover:brightness-110 disabled:opacity-60"
                  [disabled]="busy()"
                  (click)="continue()"
                >
                  {{ step() === 4 ? ('onboarding.finishSetup' | t) : ('onboarding.continue' | t) }}
                </button>
              </div>
            </div>
          }
        </div>
      </div>
    </div>
  `,
})
export class OnboardingComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly onboarding = inject(OnboardingService);
  private readonly language = inject(LanguageService);
  private readonly currentUser = inject(CurrentUserService);
  readonly branding = inject(BrandingStore);
  readonly departments = inject(DepartmentsStore);
  private readonly employees = inject(EmployeesStore);
  private readonly projects = inject(ProjectsStore);

  readonly step = signal(1);
  readonly phase = signal<'wizard' | 'ready'>('wizard');
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  readonly industries = INDUSTRIES;
  readonly timezones = TIMEZONES;

  readonly checklist = computed(() => {
    const status = this.onboarding.status();
    return [
      { key: 'onboarding.checkOrg', done: true },
      { key: 'onboarding.checkDepartment', done: !!status?.hasDepartment },
      { key: 'onboarding.checkMember', done: !!status?.hasTeamMember },
      { key: 'onboarding.checkProject', done: !!status?.hasProject },
    ];
  });

  orgName = '';
  industry = '';
  timezone = 'Asia/Riyadh';
  preferredLanguage: Lang = 'en';
  pendingLogo: File | null = null;

  departmentName = '';
  departmentDescription = '';

  memberName = '';
  memberEmail = '';
  memberRole: UserRole = 'EMPLOYEE';

  projectName = '';
  projectDepartment = '';

  async ngOnInit(): Promise<void> {
    const status = await this.onboarding.load();
    await this.branding.load();
    await this.departments.load();

    if (status) {
      this.orgName = status.organizationName;
      this.industry = status.industry ?? '';
      this.timezone = status.timezone ?? 'Asia/Riyadh';
      this.preferredLanguage = (status.preferredLanguage as Lang) || this.language.lang();
      if (status.hasDepartment && !status.hasTeamMember) this.step.set(3);
      else if (status.hasTeamMember && !status.hasProject) this.step.set(4);
      else if (status.hasProject) this.step.set(4);
    }
  }

  onLogoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.pendingLogo = file;
  }

  back(): void {
    this.error.set(null);
    this.step.update((s) => Math.max(1, s - 1));
  }

  async skip(): Promise<void> {
    await this.showReady();
  }

  async continue(): Promise<void> {
    this.error.set(null);
    this.busy.set(true);
    try {
      const ok = await this.saveCurrentStep();
      if (!ok) return;
      if (this.step() >= 4) {
        await this.showReady();
        return;
      }
      this.step.update((s) => s + 1);
    } finally {
      this.busy.set(false);
    }
  }

  async goToDashboard(): Promise<void> {
    this.busy.set(true);
    try {
      await this.router.navigate(['/dashboard']);
    } finally {
      this.busy.set(false);
    }
  }

  private async showReady(): Promise<void> {
    this.busy.set(true);
    this.error.set(null);
    try {
      const ok = await this.onboarding.complete();
      if (!ok) {
        this.error.set(this.onboarding.error() ?? 'onboarding.saveFailed');
        return;
      }
      this.phase.set('ready');
    } finally {
      this.busy.set(false);
    }
  }

  private async saveCurrentStep(): Promise<boolean> {
    switch (this.step()) {
      case 1: {
        if (!this.orgName.trim()) {
          this.error.set('onboarding.orgNameRequired');
          return false;
        }
        const saved = await this.onboarding.updateProfile({
          organizationName: this.orgName.trim(),
          industry: this.industry || null,
          timezone: this.timezone || null,
          preferredLanguage: this.preferredLanguage,
        });
        if (!saved) {
          this.error.set(this.onboarding.error() ?? 'onboarding.saveFailed');
          return false;
        }
        this.language.setLang(this.preferredLanguage);
        if (this.pendingLogo) {
          const uploaded = await this.branding.upload(this.pendingLogo);
          if (!uploaded) {
            this.error.set(this.branding.error() ?? 'settings.logoUploadFailed');
            return false;
          }
          this.pendingLogo = null;
        }
        return true;
      }
      case 2: {
        if (!this.departmentName.trim()) {
          this.error.set('onboarding.departmentRequired');
          return false;
        }
        const created = await this.departments.create(
          this.departmentName.trim(),
          this.departmentDescription.trim() || null,
        );
        if (!created) {
          this.error.set(this.departments.error() ?? 'roles.deptSaveFailed');
          return false;
        }
        this.employees.reloadDepartments();
        return true;
      }
      case 3: {
        if (!this.memberName.trim() || !this.memberEmail.trim()) {
          this.error.set('onboarding.memberRequired');
          return false;
        }
        try {
          await firstValueFrom(
            this.employees.addEmployee({
              name: this.memberName.trim(),
              email: this.memberEmail.trim(),
              role: this.memberRole,
              department: this.departments.departments()[0]?.name ?? '',
              status: 'Active',
            }),
          );
          return true;
        } catch {
          this.error.set('onboarding.memberFailed');
          return false;
        }
      }
      case 4: {
        if (!this.projectName.trim()) {
          this.error.set('onboarding.projectRequired');
          return false;
        }
        const profile = this.currentUser.profile();
        const today = new Date().toISOString().slice(0, 10);
        const end = new Date();
        end.setMonth(end.getMonth() + 1);
        try {
          await firstValueFrom(
            this.projects.addProject({
              name: this.projectName.trim(),
              department: this.projectDepartment || this.departments.departments()[0]?.name || '',
              manager: profile?.name ?? '',
              managerId: profile?.id ?? null,
              startDate: today,
              endDate: end.toISOString().slice(0, 10),
              status: 'ACTIVE',
              priority: 'MEDIUM',
            }),
          );
          return true;
        } catch {
          this.error.set('onboarding.projectFailed');
          return false;
        }
      }
      default:
        return true;
    }
  }
}
