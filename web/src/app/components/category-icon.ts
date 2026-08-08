import { Component, computed, input } from '@angular/core';

/**
 * Ícone por categoria de peça (slugs do catálogo).
 * SVGs stroke-based (24×24, currentColor) no estilo lucide.
 */
@Component({
  selector: 'app-category-icon',
  template: `
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="1.75"
      stroke-linecap="round"
      stroke-linejoin="round"
      [attr.width]="size()"
      [attr.height]="size()"
      aria-hidden="true"
    >
      @switch (slug()) {
        @case ('cpu') {
          <rect x="5" y="5" width="14" height="14" rx="2" />
          <rect x="9.5" y="9.5" width="5" height="5" rx="1" />
          <path d="M9 2v3M15 2v3M9 19v3M15 19v3M2 9h3M2 15h3M19 9h3M19 15h3" />
        }
        @case ('motherboard') {
          <rect x="3" y="3" width="18" height="18" rx="2" />
          <rect x="7" y="7" width="4.5" height="4.5" rx="1" />
          <path d="M15 7v6M18 7v6M7 15h6M7 18h6" />
        }
        @case ('gpu') {
          <rect x="2" y="6" width="18" height="11" rx="2" />
          <circle cx="9" cy="11.5" r="2.75" />
          <path d="M16 9.5h2M16 13.5h2M6 17v3M12 17v3" />
        }
        @case ('memory') {
          <path d="M3 8V6.5A1.5 1.5 0 0 1 4.5 5h15A1.5 1.5 0 0 1 21 6.5V8a2 2 0 0 0 0 4v4a1.5 1.5 0 0 1-1.5 1.5h-15A1.5 1.5 0 0 1 3 16v-4a2 2 0 0 0 0-4Z" />
          <path d="M7.5 8.5v3M12 8.5v3M16.5 8.5v3" />
        }
        @case ('storage') {
          <rect x="3" y="5" width="18" height="14" rx="2" />
          <circle cx="9" cy="12" r="3.25" />
          <circle cx="9" cy="12" r="0.5" fill="currentColor" />
          <path d="M15.5 16h2.5" />
        }
        @case ('psu') {
          <rect x="3" y="4" width="18" height="16" rx="2" />
          <circle cx="9.5" cy="12" r="4" />
          <path d="M9.5 10v4M7.8 11.2l3.4 1.6M7.8 12.8l3.4-1.6" />
          <path d="M16 9h2.5M16 12h2.5M16 15h2.5" />
        }
        @case ('case') {
          <rect x="6" y="2.5" width="12" height="19" rx="2" />
          <path d="M6 6.5h12" />
          <circle cx="12" cy="10.5" r="0.5" fill="currentColor" />
          <path d="M10 17.5h4" />
        }
        @case ('cooler') {
          <circle cx="12" cy="12" r="2" />
          <path d="M12 10c0-3 1.5-5.5 4-6M14 12c3 0 5.5 1.5 6 4M12 14c0 3-1.5 5.5-4 6M10 12c-3 0-5.5-1.5-6-4" />
        }
        @default {
          <rect x="4" y="4" width="16" height="16" rx="2" />
          <path d="M9 12h6" />
        }
      }
    </svg>
  `,
})
export class CategoryIcon {
  readonly slug = input.required<string>();
  readonly size = input(20);
}
