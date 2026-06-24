import { Injectable, computed, signal } from '@angular/core';

export interface NotificationItem {
  id: number;
  titleKey?: string;
  bodyKey?: string;
  title?: string;
  body?: string;
  params?: Record<string, string>;
  read: boolean;
  createdAt: string;
}

export interface PushNotification {
  titleKey?: string;
  bodyKey?: string;
  title?: string;
  body?: string;
  params?: Record<string, string>;
}

const INITIAL: NotificationItem[] = [
  {
    id: 1,
    titleKey: 'module.sampleNotif1Title',
    bodyKey: 'module.sampleNotif1Body',
    read: false,
    createdAt: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
  },
  {
    id: 2,
    titleKey: 'module.sampleNotif2Title',
    bodyKey: 'module.sampleNotif2Body',
    read: false,
    createdAt: new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString(),
  },
  {
    id: 3,
    titleKey: 'notifications.sample3Title',
    bodyKey: 'notifications.sample3Body',
    read: true,
    createdAt: new Date(Date.now() - 3 * 24 * 60 * 60 * 1000).toISOString(),
  },
];

@Injectable({ providedIn: 'root' })
export class NotificationsStore {
  private readonly _items = signal<NotificationItem[]>([...INITIAL]);
  private _nextId = 100;

  readonly items = this._items.asReadonly();

  readonly unreadCount = computed(() => this._items().filter((n) => !n.read).length);

  getAll(): NotificationItem[] {
    return this._items();
  }

  getRecent(limit: number): NotificationItem[] {
    return this._items().slice(0, limit);
  }

  push(input: PushNotification): NotificationItem {
    const item: NotificationItem = {
      id: ++this._nextId,
      titleKey: input.titleKey,
      bodyKey: input.bodyKey,
      title: input.title,
      body: input.body,
      params: input.params,
      read: false,
      createdAt: new Date().toISOString(),
    };
    this._items.update((list) => [item, ...list]);
    return item;
  }

  markAsRead(id: number): void {
    this._items.update((list) => list.map((n) => (n.id === id ? { ...n, read: true } : n)));
  }

  markAllRead(): void {
    this._items.update((list) => list.map((n) => ({ ...n, read: true })));
  }

  displayTitle(n: NotificationItem, t: (key: string) => string): string {
    if (n.title) return n.title;
    if (n.titleKey) return this.interpolate(t(n.titleKey), n.params);
    return '';
  }

  displayBody(n: NotificationItem, t: (key: string) => string): string {
    if (n.body) return n.body;
    if (n.bodyKey) return this.interpolate(t(n.bodyKey), n.params);
    return '';
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
    if (!params) return text;
    return Object.entries(params).reduce((s, [k, v]) => s.replace(`{{${k}}}`, v), text);
  }
}
