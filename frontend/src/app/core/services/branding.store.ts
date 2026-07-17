import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class BrandingStore {
  private readonly api = inject(ApiService);
  private readonly http = inject(HttpClient);

  readonly organizationName = signal<string | null>(null);
  readonly canUploadLogo = signal(false);
  readonly logoObjectUrl = signal<string | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  private objectUrl: string | null = null;

  async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const branding = await firstValueFrom(this.api.getOrganizationBranding());
      this.organizationName.set(branding.organizationName);
      this.canUploadLogo.set(branding.canUploadLogo);
      if (branding.logoUrl) {
        await this.refreshLogoBlob(branding.logoUrl);
      } else {
        this.clearObjectUrl();
      }
    } catch {
      this.error.set('settings.logoLoadFailed');
      this.canUploadLogo.set(false);
      this.clearObjectUrl();
    } finally {
      this.loading.set(false);
    }
  }

  async upload(file: File): Promise<boolean> {
    this.saving.set(true);
    this.error.set(null);
    try {
      const result = await firstValueFrom(this.api.uploadOrganizationLogo(file));
      this.canUploadLogo.set(true);
      await this.refreshLogoBlob(result.logoUrl);
      return true;
    } catch {
      this.error.set('settings.logoUploadFailed');
      return false;
    } finally {
      this.saving.set(false);
    }
  }

  async clear(): Promise<boolean> {
    this.saving.set(true);
    this.error.set(null);
    try {
      await firstValueFrom(this.api.clearOrganizationLogo());
      this.clearObjectUrl();
      return true;
    } catch {
      this.error.set('settings.logoClearFailed');
      return false;
    } finally {
      this.saving.set(false);
    }
  }

  private async refreshLogoBlob(logoUrl: string): Promise<void> {
    const url = logoUrl.startsWith('/') ? logoUrl : `${environment.apiUrl}/${logoUrl}`;
    const blob = await firstValueFrom(this.http.get(url, { responseType: 'blob' }));
    this.clearObjectUrl();
    this.objectUrl = URL.createObjectURL(blob);
    this.logoObjectUrl.set(this.objectUrl);
  }

  private clearObjectUrl(): void {
    if (this.objectUrl) {
      URL.revokeObjectURL(this.objectUrl);
      this.objectUrl = null;
    }
    this.logoObjectUrl.set(null);
  }
}
