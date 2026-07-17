import { Component, computed, inject, input } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { STRATIX_ICON_PATHS, StratixIconName } from '../../icons/stratix-icons';

const SIZE_CLASS: Record<string, string> = {
  xs: 'h-3.5 w-3.5',
  sm: 'h-4 w-4',
  md: 'h-5 w-5',
  lg: 'h-6 w-6',
  xl: 'h-8 w-8',
  '2xl': 'h-10 w-10',
};

@Component({
  selector: 'app-ui-icon',
  standalone: true,
  template: `
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="2"
      stroke-linecap="round"
      stroke-linejoin="round"
      [attr.class]="svgClass()"
      aria-hidden="true"
      [innerHTML]="paths()"
    ></svg>
  `,
})
export class UiIconComponent {
  private readonly sanitizer = inject(DomSanitizer);

  readonly name = input.required<StratixIconName | string>();
  /** xs | sm | md | lg | xl | 2xl — or pass a custom class via `className`. */
  readonly size = input<string>('md');
  readonly className = input('');

  readonly svgClass = computed(() => {
    const sizeCls = SIZE_CLASS[this.size()] ?? this.size();
    return `${sizeCls} shrink-0 ${this.className()}`.trim();
  });

  readonly paths = computed((): SafeHtml => {
    const key = this.name();
    const inner = STRATIX_ICON_PATHS[key] ?? STRATIX_ICON_PATHS['circle'];
    return this.sanitizer.bypassSecurityTrustHtml(inner);
  });
}
