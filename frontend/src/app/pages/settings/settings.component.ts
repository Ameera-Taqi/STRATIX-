import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { CurrentUserService } from '../../core/services/current-user.service';
import { AuthService } from '../../core/services/auth.service';
import { EmployeesStore } from '../../core/services/employees.store';
import { BrandingStore } from '../../core/services/branding.store';
import { UiIconComponent } from '../../shared/components/ui-icon/ui-icon.component';
import { employeeInitials } from '../../shared/utils/employee.util';
import { roleLabelKey } from '../../core/config/stratix-roles';

interface NotificationPrefs {
  emailAlerts: boolean;
  inAppAlerts: boolean;
}

const PREFS_STORAGE_KEY = 'stratix.notificationPrefs';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [TopbarComponent, TranslatePipe, FormsModule, RouterLink, UiIconComponent],
  templateUrl: './settings.component.html',
})
export class SettingsComponent implements OnInit {
  private readonly currentUser = inject(CurrentUserService);
  private readonly auth = inject(AuthService);
  private readonly employeesStore = inject(EmployeesStore);
  private readonly branding = inject(BrandingStore);

  readonly profile = this.currentUser.profile;
  readonly departments = this.employeesStore.departmentOptions;
  readonly initials = employeeInitials;
  readonly roleKey = roleLabelKey;

  readonly canUploadLogo = this.branding.canUploadLogo;
  readonly logoUrl = this.branding.logoObjectUrl;
  readonly logoSaving = this.branding.saving;
  readonly logoError = this.branding.error;

  form = {
    name: '',
    email: '',
    department: '',
  };

  readonly saving = signal(false);
  readonly saved = signal(false);
  readonly error = signal<string | null>(null);
  readonly logoSaved = signal(false);

  readonly prefs = signal<NotificationPrefs>(this.loadPrefs());
  readonly prefsSaved = signal(false);
  private prefsSavedTimeout?: ReturnType<typeof window.setTimeout>;

  ngOnInit(): void {
    if (!this.employeesStore.loaded()) {
      this.employeesStore.loadFromApi();
    }
    void this.branding.load();
    this.syncFormFromProfile();
  }

  async onLogoSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    const ok = await this.branding.upload(file);
    if (ok) {
      this.logoSaved.set(true);
      window.setTimeout(() => this.logoSaved.set(false), 2500);
    }
  }

  async clearLogo(): Promise<void> {
    const ok = await this.branding.clear();
    if (ok) {
      this.logoSaved.set(true);
      window.setTimeout(() => this.logoSaved.set(false), 2500);
    }
  }

  togglePref(key: keyof NotificationPrefs): void {
    const next = { ...this.prefs(), [key]: !this.prefs()[key] };
    this.prefs.set(next);
    try {
      localStorage.setItem(PREFS_STORAGE_KEY, JSON.stringify(next));
    } catch {
      // localStorage unavailable (e.g. private mode); ignore persistence failure.
    }
    this.prefsSaved.set(true);
    if (this.prefsSavedTimeout) window.clearTimeout(this.prefsSavedTimeout);
    this.prefsSavedTimeout = window.setTimeout(() => this.prefsSaved.set(false), 2000);
  }

  private loadPrefs(): NotificationPrefs {
    const defaults: NotificationPrefs = { emailAlerts: true, inAppAlerts: true };
    try {
      const raw = localStorage.getItem(PREFS_STORAGE_KEY);
      if (!raw) return defaults;
      const parsed = JSON.parse(raw);
      return {
        emailAlerts: typeof parsed.emailAlerts === 'boolean' ? parsed.emailAlerts : defaults.emailAlerts,
        inAppAlerts: typeof parsed.inAppAlerts === 'boolean' ? parsed.inAppAlerts : defaults.inAppAlerts,
      };
    } catch {
      return defaults;
    }
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
