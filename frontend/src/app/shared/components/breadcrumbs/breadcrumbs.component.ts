import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

export interface BreadcrumbItem {
  /** Visible label (dynamic names: project, feature, task). */
  label?: string;
  /** i18n key when the crumb is a static root (Projects, Risks, Team). */
  labelKey?: string;
  /** Router link — omit on the current (last) crumb. */
  link?: string | (string | number)[];
  /** Optional query params (e.g. featureId, tab). */
  queryParams?: Record<string, string | number | null>;
}

@Component({
  selector: 'app-breadcrumbs',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  template: `
    <nav class="mb-4" [attr.aria-label]="'breadcrumb.nav' | t">
      <ol class="flex flex-wrap items-center gap-1.5 text-sm">
        @for (item of items(); track $index; let last = $last; let first = $first) {
          @if (!first) {
            <li class="select-none text-slate-300 dark:text-slate-600" aria-hidden="true">/</li>
          }
          <li class="min-w-0 max-w-[12rem] truncate sm:max-w-[16rem]">
            @if (!last && item.link) {
              <a
                [routerLink]="item.link"
                [queryParams]="item.queryParams ?? null"
                class="font-medium text-primary hover:underline"
              >
                @if (item.labelKey) {
                  {{ item.labelKey | t }}
                } @else {
                  {{ item.label }}
                }
              </a>
            } @else {
              <span
                class="font-medium"
                [class]="last ? 'text-dark dark:text-slate-100' : 'stratix-muted'"
                [attr.aria-current]="last ? 'page' : null"
              >
                @if (item.labelKey) {
                  {{ item.labelKey | t }}
                } @else {
                  {{ item.label }}
                }
              </span>
            }
          </li>
        }
      </ol>
    </nav>
  `,
})
export class BreadcrumbsComponent {
  readonly items = input.required<BreadcrumbItem[]>();
}
