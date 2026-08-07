import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { AuditLogStore } from '../../core/services/audit-log.store';
import { AuditEntity } from '../../core/models/audit.model';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { UiIconComponent } from '../../shared/components/ui-icon/ui-icon.component';

@Component({
  selector: 'app-audit-log',
  standalone: true,
  imports: [TopbarComponent, RouterLink, TranslatePipe, FormsModule, UiIconComponent],
  templateUrl: './audit-log.component.html',
  styles: `
    :host {
      display: flex;
      flex: 1 1 auto;
      flex-direction: column;
      min-height: 0;
      overflow: hidden;
    }
  `,
})
export class AuditLogComponent implements OnInit {
  private readonly audit = inject(AuditLogStore);

  readonly entityFilter = signal<AuditEntity | 'ALL'>('ALL');
  readonly actionFilter = signal('ALL');
  readonly actorFilter = signal('');
  readonly search = signal('');
  readonly startDate = signal('');
  readonly endDate = signal('');
  readonly page = signal(1);

  readonly loading = this.audit.loading;
  readonly error = this.audit.error;
  readonly entries = this.audit.entries;
  readonly totalElements = this.audit.totalElements;
  readonly totalPages = this.audit.totalPages;

  readonly actors = computed(() => this.audit.actors());

  readonly stats = computed(() => {
    const list = this.entries();
    return {
      total: this.totalElements(),
      creates: list.filter((e) => e.action === 'CREATE').length,
      updates: list.filter(
        (e) =>
          e.action === 'UPDATE' ||
          e.action === 'STATUS_CHANGE' ||
          e.action === 'ASSIGNMENT_CHANGE' ||
          e.action === 'PRIORITY_CHANGE',
      ).length,
      today: list.filter((e) => e.at.slice(0, 10) === new Date().toISOString().slice(0, 10)).length,
    };
  });

  readonly entityOptions: (AuditEntity | 'ALL')[] = [
    'ALL',
    'PROJECT',
    'TASK',
    'STAGE',
    'RISK',
    'USER',
  ];

  readonly actionOptions = [
    'ALL',
    'CREATE',
    'UPDATE',
    'DELETE',
    'STATUS_CHANGE',
    'ASSIGNMENT_CHANGE',
    'PRIORITY_CHANGE',
  ];

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.audit.loadFromApi(this.currentQuery());
  }

  onFilterChange(): void {
    this.page.set(1);
    this.reload();
  }

  onSearchInput(value: string): void {
    this.search.set(value);
    this.page.set(1);
    this.reload();
  }

  prevPage(): void {
    if (this.page() <= 1) return;
    this.page.update((p) => p - 1);
    this.reload();
  }

  nextPage(): void {
    if (this.page() >= this.totalPages()) return;
    this.page.update((p) => p + 1);
    this.reload();
  }

  formatAt(iso: string): string {
    return new Date(iso).toLocaleString();
  }

  formatValue(value?: string): string {
    return value?.replace(/_/g, ' ') ?? '—';
  }

  entityLink(entry: { entityType: AuditEntity; entityId: number; projectId?: number | null }): string[] | null {
    switch (entry.entityType) {
      case 'PROJECT':
        return ['/projects', String(entry.entityId)];
      case 'TASK':
        return ['/tasks', String(entry.entityId)];
      case 'RISK':
        return ['/risks', String(entry.entityId)];
      case 'USER':
        return ['/team', String(entry.entityId)];
      case 'STAGE':
      case 'MILESTONE':
      case 'ATTACHMENT':
        return entry.projectId ? ['/projects', String(entry.projectId)] : null;
      default:
        return null;
    }
  }

  private currentQuery() {
    return {
      entityType: this.entityFilter(),
      action: this.actionFilter() as 'ALL' | import('../../core/models/audit.model').AuditAction,
      search: this.search(),
      startDate: this.startDate() || undefined,
      endDate: this.endDate() || undefined,
      page: this.page(),
      size: 25,
    };
  }
}
