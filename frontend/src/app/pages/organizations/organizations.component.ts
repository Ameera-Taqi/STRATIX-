import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { RoleAccessService } from '../../core/services/role-access.service';
import { OrganizationsStore } from '../../core/services/organizations.store';
import {
  OrganizationStatus,
  SubscriptionPlanCode,
} from '../../core/models/organization.model';
import { UiIconComponent } from '../../shared/components/ui-icon/ui-icon.component';

type OrgsTab = 'organizations' | 'plans';

@Component({
  selector: 'app-organizations',
  standalone: true,
  imports: [TopbarComponent, TranslatePipe, FormsModule, UiIconComponent, RouterLink],
  templateUrl: './organizations.component.html',
})
export class OrganizationsComponent implements OnInit {
  private readonly store = inject(OrganizationsStore);
  private readonly roleAccess = inject(RoleAccessService);
  private readonly router = inject(Router);

  readonly organizations = this.store.organizations;
  readonly plans = this.store.plans;
  readonly loading = this.store.loading;
  readonly error = this.store.error;

  readonly tab = signal<OrgsTab>('organizations');
  readonly search = signal('');
  readonly savingId = signal<number | null>(null);
  readonly actionError = signal<string | null>(null);

  readonly showAddModal = signal(false);
  readonly submitting = signal(false);
  readonly formError = signal<string | null>(null);

  readonly statusOptions: OrganizationStatus[] = ['ACTIVE', 'SUSPENDED', 'CANCELLED'];
  readonly planOptions: SubscriptionPlanCode[] = ['FREE', 'PRO', 'ENTERPRISE'];

  form = {
    organizationName: '',
    adminName: '',
    adminEmail: '',
    password: '',
    slug: '',
    subscriptionPlan: 'FREE' as SubscriptionPlanCode,
  };

  readonly isSuperAdmin = computed(() => this.roleAccess.role() === 'SUPER_ADMIN');

  readonly filtered = computed(() => {
    const q = this.search().toLowerCase().trim();
    return this.organizations().filter((o) => {
      if (!q) return true;
      return (
        o.name.toLowerCase().includes(q) ||
        o.slug.toLowerCase().includes(q) ||
        o.status.toLowerCase().includes(q) ||
        o.subscriptionPlan.toLowerCase().includes(q)
      );
    });
  });

  readonly stats = computed(() => {
    const list = this.organizations();
    return {
      total: list.length,
      active: list.filter((o) => o.status === 'ACTIVE').length,
      suspended: list.filter((o) => o.status === 'SUSPENDED').length,
    };
  });

  ngOnInit(): void {
    if (!this.isSuperAdmin()) {
      void this.router.navigateByUrl('/dashboard');
      return;
    }
    void this.store.loadAll();
  }

  setTab(tab: OrgsTab): void {
    this.tab.set(tab);
    this.actionError.set(null);
  }

  statusLabelKey(status: string): string {
    switch (status) {
      case 'ACTIVE':
        return 'orgs.statusActive';
      case 'SUSPENDED':
        return 'orgs.statusSuspended';
      case 'CANCELLED':
        return 'orgs.statusCancelled';
      default:
        return 'common.status';
    }
  }

  statusSegClass(current: string, option: OrganizationStatus): string {
    const selected = current === option;
    if (!selected) {
      return 'border-transparent text-slate-500 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-700/60';
    }
    switch (option) {
      case 'ACTIVE':
        return 'border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-300';
      case 'SUSPENDED':
        return 'border-amber-200 bg-amber-50 text-amber-800 dark:border-amber-800 dark:bg-amber-900/40 dark:text-amber-300';
      case 'CANCELLED':
        return 'border-rose-200 bg-rose-50 text-rose-700 dark:border-rose-800 dark:bg-rose-900/40 dark:text-rose-300';
    }
  }

  openAddModal(): void {
    this.formError.set(null);
    this.form = {
      organizationName: '',
      adminName: '',
      adminEmail: '',
      password: '',
      slug: '',
      subscriptionPlan: 'FREE',
    };
    this.showAddModal.set(true);
  }

  closeAddModal(): void {
    this.showAddModal.set(false);
    this.formError.set(null);
  }

  async saveOrganization(): Promise<void> {
    if (!this.form.organizationName.trim() || !this.form.adminName.trim() || !this.form.adminEmail.trim()) {
      this.formError.set('orgs.createRequired');
      return;
    }
    if (this.form.password.length < 8) {
      this.formError.set('orgs.createWeakPassword');
      return;
    }

    this.submitting.set(true);
    this.formError.set(null);
    try {
      await this.store.createOrganization({
        organizationName: this.form.organizationName.trim(),
        adminName: this.form.adminName.trim(),
        adminEmail: this.form.adminEmail.trim(),
        password: this.form.password,
        slug: this.form.slug.trim() || null,
        subscriptionPlan: this.form.subscriptionPlan,
      });
      this.closeAddModal();
    } catch (err) {
      const msg =
        err instanceof HttpErrorResponse && typeof err.error?.message === 'string'
          ? err.error.message
          : null;
      this.formError.set(msg === 'Email already in use.' ? 'orgs.createEmailInUse' : 'orgs.createFailed');
    } finally {
      this.submitting.set(false);
    }
  }

  async onStatusChange(id: number, status: OrganizationStatus, current: string): Promise<void> {
    if (status === current || this.savingId() === id) return;
    await this.patchOrg(id, { status });
  }

  async onPlanChange(id: number, subscriptionPlan: string): Promise<void> {
    await this.patchOrg(id, { subscriptionPlan });
  }

  private async patchOrg(
    id: number,
    body: { status?: string; subscriptionPlan?: string },
  ): Promise<void> {
    this.savingId.set(id);
    this.actionError.set(null);
    try {
      await this.store.updateOrganization(id, body);
    } catch {
      this.actionError.set('orgs.updateFailed');
      await this.store.loadAll();
    } finally {
      this.savingId.set(null);
    }
  }

  async deleteOrganization(org: { id: number; name: string }): Promise<void> {
    if (this.savingId() === org.id) return;
    if (!confirm(`Delete organization "${org.name}"? This cannot be undone.`)) return;

    this.savingId.set(org.id);
    this.actionError.set(null);
    try {
      await this.store.deleteOrganization(org.id);
    } catch {
      this.actionError.set('orgs.deleteFailed');
    } finally {
      this.savingId.set(null);
    }
  }
}
