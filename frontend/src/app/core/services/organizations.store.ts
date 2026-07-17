import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import {
  CreateOrganizationRequest,
  OrganizationRow,
  PlanRow,
  UpdateOrganizationRequest,
} from '../models/organization.model';

@Injectable({ providedIn: 'root' })
export class OrganizationsStore {
  private readonly api = inject(ApiService);

  readonly organizations = signal<OrganizationRow[]>([]);
  readonly plans = signal<PlanRow[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  async loadAll(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const [orgs, plans] = await Promise.all([
        firstValueFrom(this.api.getOrganizations()),
        firstValueFrom(this.api.getPlans()),
      ]);
      this.organizations.set(orgs);
      this.plans.set(plans);
    } catch {
      this.error.set('Failed to load organizations');
    } finally {
      this.loading.set(false);
    }
  }

  async createOrganization(body: CreateOrganizationRequest): Promise<OrganizationRow> {
    const created = await firstValueFrom(this.api.createOrganization(body));
    this.organizations.update((list) => [...list, created].sort((a, b) => a.id - b.id));
    return created;
  }

  async updateOrganization(id: number, body: UpdateOrganizationRequest): Promise<void> {
    const updated = await firstValueFrom(this.api.updateOrganization(id, body));
    this.organizations.update((list) => list.map((o) => (o.id === id ? updated : o)));
  }

  async deleteOrganization(id: number): Promise<void> {
    await firstValueFrom(this.api.deleteOrganization(id));
    this.organizations.update((list) => list.filter((o) => o.id !== id));
  }
}
