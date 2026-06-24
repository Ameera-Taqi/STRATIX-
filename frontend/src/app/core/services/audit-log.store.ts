import { Injectable, inject, signal } from '@angular/core';
import { Observable, catchError, tap, throwError } from 'rxjs';
import {
  AuditEntry,
  AuditEntity,
  AuditLogInput,
  AuditLogPage,
  AuditLogQuery,
} from '../models/audit.model';
import { ApiService } from './api.service';
import { CurrentUserService } from './current-user.service';

/** Local-only entries for client-side features not yet persisted on the server (comments, attachments, export). */
const ENABLE_LOCAL_AUDIT_FALLBACK = false;

@Injectable({ providedIn: 'root' })
export class AuditLogStore {
  private readonly api = inject(ApiService);
  private readonly currentUser = inject(CurrentUserService);

  private readonly _entries = signal<AuditEntry[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _loaded = signal(false);
  private readonly _totalElements = signal(0);
  private readonly _totalPages = signal(0);
  private readonly _page = signal(0);
  private readonly _localEntries = signal<AuditEntry[]>([]);
  private _localNextId = 1;

  readonly entries = this._entries.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly loaded = this._loaded.asReadonly();
  readonly totalElements = this._totalElements.asReadonly();
  readonly totalPages = this._totalPages.asReadonly();
  readonly page = this._page.asReadonly();

  loadFromApi(query: AuditLogQuery = {}): void {
    this._loading.set(true);
    this._error.set(null);

    this.api.getAuditLogs(this.toParams(query)).subscribe({
      next: (page) => {
        this.applyPage(page);
        this._loading.set(false);
        this._loaded.set(true);
      },
      error: () => {
        this._loading.set(false);
        this._loaded.set(true);
        if (ENABLE_LOCAL_AUDIT_FALLBACK) {
          // Temporary fallback — disabled by default; server audit is the source of truth.
          this._entries.set(this._localEntries());
          this._error.set('audit.errorFallback');
        } else {
          this._entries.set([]);
          this._error.set('audit.errorLoad');
        }
      },
    });
  }

  refresh(query: AuditLogQuery = {}): Observable<AuditLogPage> {
    this._loading.set(true);
    this._error.set(null);
    return this.api.getAuditLogs(this.toParams(query)).pipe(
      tap((page) => {
        this.applyPage(page);
        this._loading.set(false);
        this._loaded.set(true);
      }),
      catchError((err) => {
        this._loading.set(false);
        this._error.set('audit.errorLoad');
        return throwError(() => err);
      }),
    );
  }

  /** Client-only audit for features without server persistence yet. */
  log(input: AuditLogInput): AuditEntry {
    const profile = this.currentUser.profile();
    const entry: AuditEntry = {
      id: this._localNextId++,
      entityType: input.entityType,
      entityId: input.entityId,
      entityLabel: input.entityLabel,
      action: input.action,
      actor: input.actor ?? profile.name,
      actorRole: profile.role,
      projectId: input.projectId,
      projectName: input.projectName,
      field: input.field,
      oldValue: input.oldValue,
      newValue: input.newValue,
      details: input.details,
      activityKey: input.activityKey,
      at: new Date().toISOString(),
    };
    this._localEntries.update((list) => [entry, ...list]);
    return entry;
  }

  forEntity(entityType: AuditEntity, entityId: number): AuditEntry[] {
    return this._entries()
      .filter((e) => e.entityType === entityType && e.entityId === entityId)
      .sort((a, b) => b.at.localeCompare(a.at));
  }

  forProject(projectId: number): AuditEntry[] {
    return this._entries()
      .filter(
        (e) =>
          e.projectId === projectId ||
          (e.entityType === 'PROJECT' && e.entityId === projectId),
      )
      .sort((a, b) => b.at.localeCompare(a.at));
  }

  actors(): string[] {
    return [...new Set(this._entries().map((e) => e.actor).filter(Boolean))].sort();
  }

  private applyPage(page: AuditLogPage): void {
    this._entries.set(page.content);
    this._totalElements.set(page.totalElements);
    this._totalPages.set(page.totalPages);
    this._page.set(page.page);
  }

  private toParams(query: AuditLogQuery): Record<string, string | number> {
    const params: Record<string, string | number> = {
      page: query.page ?? 0,
      size: query.size ?? 25,
    };
    if (query.entityType && query.entityType !== 'ALL') params['entityType'] = query.entityType;
    if (query.entityId != null) params['entityId'] = query.entityId;
    if (query.userId != null) params['userId'] = query.userId;
    if (query.projectId != null) params['projectId'] = query.projectId;
    if (query.action && query.action !== 'ALL') params['action'] = query.action;
    if (query.startDate) params['startDate'] = query.startDate;
    if (query.endDate) params['endDate'] = query.endDate;
    if (query.search?.trim()) params['search'] = query.search.trim();
    return params;
  }
}
