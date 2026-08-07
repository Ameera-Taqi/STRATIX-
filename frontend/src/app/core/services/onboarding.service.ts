import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { CurrentUserService } from './current-user.service';
import {
  OrganizationOnboardingStatus,
  UpdateOrganizationProfileRequest,
} from '../models/onboarding.model';

@Injectable({ providedIn: 'root' })
export class OnboardingService {
  private readonly api = inject(ApiService);
  private readonly currentUser = inject(CurrentUserService);

  readonly status = signal<OrganizationOnboardingStatus | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  /** ORG_ADMIN / ADMIN who have not finished or skipped the wizard. */
  needsOnboarding(): boolean {
    const role = this.currentUser.profile()?.roleCode;
    if (role !== 'ORG_ADMIN' && role !== 'ADMIN') return false;
    const status = this.status();
    if (status) return !status.onboardingCompleted;
    return true; // unknown until loaded — callers should await load()
  }

  async load(): Promise<OrganizationOnboardingStatus | null> {
    const role = this.currentUser.profile()?.roleCode;
    if (role !== 'ORG_ADMIN' && role !== 'ADMIN') {
      this.status.set(null);
      return null;
    }

    this.loading.set(true);
    this.error.set(null);
    try {
      const status = await firstValueFrom(this.api.getOrganizationOnboarding());
      this.status.set(status);
      return status;
    } catch {
      this.error.set('onboarding.loadFailed');
      return null;
    } finally {
      this.loading.set(false);
    }
  }

  async updateProfile(body: UpdateOrganizationProfileRequest): Promise<boolean> {
    this.saving.set(true);
    this.error.set(null);
    try {
      const status = await firstValueFrom(this.api.updateOrganizationProfile(body));
      this.status.set(status);
      return true;
    } catch {
      this.error.set('onboarding.saveFailed');
      return false;
    } finally {
      this.saving.set(false);
    }
  }

  async complete(): Promise<boolean> {
    this.saving.set(true);
    this.error.set(null);
    try {
      const status = await firstValueFrom(this.api.completeOrganizationOnboarding());
      this.status.set(status);
      return true;
    } catch {
      this.error.set('onboarding.saveFailed');
      return false;
    } finally {
      this.saving.set(false);
    }
  }

  /** Destination after login/register for the current session. */
  async resolveHomePath(): Promise<string> {
    const role = this.currentUser.profile()?.roleCode;
    if (role !== 'ORG_ADMIN' && role !== 'ADMIN') return '/dashboard';

    const status = this.status() ?? (await this.load());
    if (status && !status.onboardingCompleted) return '/onboarding';
    return '/dashboard';
  }
}
