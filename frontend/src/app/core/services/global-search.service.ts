import { Injectable, computed, inject, signal } from '@angular/core';
import { ProjectsStore } from './projects.store';
import { TasksStore } from './tasks.store';
import { EmployeesStore } from './employees.store';

export type GlobalSearchGroup = 'projects' | 'tasks' | 'people';

export interface GlobalSearchHit {
  id: number;
  group: GlobalSearchGroup;
  title: string;
  subtitle?: string;
  route: (string | number)[];
}

export interface GlobalSearchResults {
  query: string;
  projects: GlobalSearchHit[];
  tasks: GlobalSearchHit[];
  people: GlobalSearchHit[];
}

const LIMIT_PER_GROUP = 5;

@Injectable({ providedIn: 'root' })
export class GlobalSearchService {
  private readonly projects = inject(ProjectsStore);
  private readonly tasks = inject(TasksStore);
  private readonly employees = inject(EmployeesStore);

  private readonly _query = signal('');
  private readonly _open = signal(false);

  readonly query = this._query.asReadonly();
  readonly open = this._open.asReadonly();

  readonly results = computed((): GlobalSearchResults => {
    const raw = this._query().trim();
    const q = raw.toLowerCase();
    if (q.length < 1) {
      return { query: raw, projects: [], tasks: [], people: [] };
    }

    const projects = this.projects
      .projects()
      .filter(
        (p) =>
          p.name.toLowerCase().includes(q) ||
          (p.department || '').toLowerCase().includes(q) ||
          (p.manager || '').toLowerCase().includes(q),
      )
      .slice(0, LIMIT_PER_GROUP)
      .map(
        (p): GlobalSearchHit => ({
          id: p.id,
          group: 'projects',
          title: p.name,
          subtitle: p.department || p.status,
          route: ['/projects', p.id],
        }),
      );

    const tasks = this.tasks
      .getAll()
      .filter(
        (t) =>
          t.title.toLowerCase().includes(q) ||
          (t.assignee || '').toLowerCase().includes(q) ||
          (t.projectName || '').toLowerCase().includes(q),
      )
      .slice(0, LIMIT_PER_GROUP)
      .map(
        (t): GlobalSearchHit => ({
          id: t.id,
          group: 'tasks',
          title: t.title,
          subtitle: t.projectName || t.status,
          route: ['/tasks', t.id],
        }),
      );

    const people = this.employees
      .employees()
      .filter(
        (e) =>
          e.name.toLowerCase().includes(q) ||
          (e.email || '').toLowerCase().includes(q) ||
          (e.department || '').toLowerCase().includes(q) ||
          (e.role || '').toLowerCase().includes(q),
      )
      .slice(0, LIMIT_PER_GROUP)
      .map(
        (e): GlobalSearchHit => ({
          id: e.id,
          group: 'people',
          title: e.name,
          subtitle: e.department || e.role,
          route: ['/team', e.id],
        }),
      );

    return { query: raw, projects, tasks, people };
  });

  readonly hasResults = computed(() => {
    const r = this.results();
    return r.projects.length + r.tasks.length + r.people.length > 0;
  });

  readonly isEmptyQuery = computed(() => this._query().trim().length === 0);

  ensureDataLoaded(): void {
    if (!this.projects.loaded()) {
      this.projects.loadFromApi(() => this.tasks.loadFromApi());
    } else if (!this.tasks.loaded()) {
      this.tasks.loadFromApi();
    }
    if (!this.employees.loaded()) {
      this.employees.loadFromApi();
    }
  }

  setQuery(value: string): void {
    this._query.set(value);
    if (value.trim()) {
      this._open.set(true);
      this.ensureDataLoaded();
    }
  }

  openPanel(): void {
    this._open.set(true);
    this.ensureDataLoaded();
  }

  closePanel(): void {
    this._open.set(false);
  }

  clear(): void {
    this._query.set('');
    this._open.set(false);
  }
}
