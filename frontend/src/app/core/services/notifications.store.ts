import { Injectable, computed, inject, signal } from '@angular/core';
import { ApiService } from './api.service';
import { CurrentUserService } from './current-user.service';
import { LanguageService } from '../i18n/language.service';
import { NotificationApiResponse, NotificationApiType } from '../models/notification.model';

export interface NotificationItem {
  id: number;
  title: string;
  body: string;
  read: boolean;
  createdAt: string;
  link?: string | null;
  type?: string;
}

/** Fire-and-forget local notification — resolved to plain text and persisted for the current user. */
export interface PushNotification {
  titleKey?: string;
  bodyKey?: string;
  title?: string;
  body?: string;
  params?: Record<string, string>;
  type?: NotificationApiType;
  link?: string | null;
}

function fromApi(n: NotificationApiResponse): NotificationItem {
  return {
    id: n.id,
    title: n.title,
    body: n.message ?? '',
    read: n.isRead,
    createdAt: n.createdAt,
    link: n.link,
    type: n.type,
  };
}

@Injectable({ providedIn: 'root' })
export class NotificationsStore {
  private readonly api = inject(ApiService);
  private readonly lang = inject(LanguageService);
  private readonly currentUser = inject(CurrentUserService);

  private readonly _items = signal<NotificationItem[]>([]);
  private readonly _loaded = signal(false);
  private readonly _loading = signal(false);

  readonly items = this._items.asReadonly();
  readonly loaded = this._loaded.asReadonly();
  readonly loading = this._loading.asReadonly();

  readonly unreadCount = computed(() => this._items().filter((n) => !n.read).length);

  loadFromApi(): void {
    if (this._loading()) return;
    this._loading.set(true);
    this.api.getNotifications().subscribe({
      next: (list) => {
        this._items.set(
          list
            .slice()
            .sort((a, b) => b.createdAt.localeCompare(a.createdAt))
            .map(fromApi),
        );
        this._loaded.set(true);
        this._loading.set(false);
      },
      error: () => {
        this._loaded.set(true);
        this._loading.set(false);
      },
    });
  }

  getAll(): NotificationItem[] {
    return this._items();
  }

  getRecent(limit: number): NotificationItem[] {
    return this._items().slice(0, limit);
  }

  /** Records a notification for the current user about an action they just took (optimistic + best-effort API persist). */
  push(input: PushNotification): void {
    const title = input.title ?? this.interpolate(this.lang.t(input.titleKey ?? ''), input.params);
    const body = input.body ?? this.interpolate(this.lang.t(input.bodyKey ?? ''), input.params);

    const optimistic: NotificationItem = {
      id: -Date.now(),
      title,
      body,
      read: false,
      createdAt: new Date().toISOString(),
      link: input.link,
      type: input.type,
    };
    this._items.update((list) => [optimistic, ...list]);

    this.api
      .createNotification({
        userId: this.currentUser.profile().id,
        title,
        message: body,
        type: input.type ?? 'INFO',
        link: input.link,
      })
      .subscribe({
        next: (created) => {
          this._items.update((list) =>
            list.map((n) => (n.id === optimistic.id ? fromApi(created) : n)),
          );
        },
        error: () => {
          /* Notification creation is best-effort — the triggering action already succeeded locally. */
        },
      });
  }

  markAsRead(id: number): void {
    const previous = this._items();
    this._items.update((list) => list.map((n) => (n.id === id ? { ...n, read: true } : n)));
    if (id < 0) return;
    this.api.markNotificationRead(id).subscribe({
      error: () => this._items.set(previous),
    });
  }

  markAllRead(): void {
    const previous = this._items();
    this._items.update((list) => list.map((n) => ({ ...n, read: true })));
    this.api.markAllNotificationsRead().subscribe({
      error: () => this._items.set(previous),
    });
  }

  remove(id: number): void {
    const previous = this._items();
    this._items.update((list) => list.filter((n) => n.id !== id));
    if (id < 0) return;
    this.api.deleteNotification(id).subscribe({
      error: () => this._items.set(previous),
    });
  }

  displayTitle(n: NotificationItem): string {
    return n.title;
  }

  displayBody(n: NotificationItem): string {
    return n.body;
  }

  timeAgo(iso: string): string {
    const diff = Date.now() - new Date(iso).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 1) return 'now';
    if (mins < 60) return `${mins}m`;
    const hours = Math.floor(mins / 60);
    if (hours < 24) return `${hours}h`;
    const days = Math.floor(hours / 24);
    return `${days}d`;
  }

  private interpolate(text: string, params?: Record<string, string>): string {
    if (!text || !params) return text;
    return Object.entries(params).reduce((s, [k, v]) => s.replace(`{{${k}}}`, v), text);
  }
}
