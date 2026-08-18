import { Component, computed, HostListener, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { httpResource } from '@angular/common/http';
import { NAV_CATEGORIES, type ProductsResponse } from './api';
import { CategoryIcon } from './components/category-icon/category-icon';
import { BuildState } from './build-state';
import { formatBRL } from './format';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CategoryIcon],
  templateUrl: './app.html',
})
export class App {
  private readonly router = inject(Router);
  private readonly buildState = inject(BuildState);

  /** Categorias estáticas no primeiro paint — a API não empurra o nav. */
  protected readonly navCategories = NAV_CATEGORIES;

  protected readonly menuOpen = signal(false);
  protected readonly mobileSearch = signal(false);
  protected readonly searchOpen = signal(false);
  protected readonly searchInput = signal('');
  protected readonly searchQ = signal('');

  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  protected readonly searchResults = httpResource<ProductsResponse>(() => {
    const q = this.searchQ();
    if (q.length < 2) return undefined;
    return `/api/v1/products?q=${encodeURIComponent(q)}&limit=6&sort=price_asc`;
  });

  protected readonly buildCount = computed(() => this.buildState.build()?.items.length ?? 0);
  protected readonly formatBRL = formatBRL;

  constructor() {
    this.router.events.subscribe((e) => {
      if (e instanceof NavigationEnd) this.closeOverlays();
    });
  }

  protected onSearchInput(value: string): void {
    this.searchInput.set(value);
    this.searchOpen.set(true);
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.searchQ.set(value.trim()), 180);
  }

  protected goSearch(): void {
    const q = this.searchInput().trim();
    if (!q) return;
    void this.router.navigate(['/busca'], { queryParams: { q } });
    this.closeOverlays();
  }

  protected toggleMobileSearch(): void {
    this.mobileSearch.update((v) => !v);
    this.searchOpen.set(true);
  }

  protected closeOverlays(): void {
    this.menuOpen.set(false);
    this.mobileSearch.set(false);
    this.searchOpen.set(false);
    document.body.style.overflow = '';
  }

  protected openMenu(): void {
    this.menuOpen.set(true);
    document.body.style.overflow = 'hidden';
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    this.closeOverlays();
  }

  @HostListener('document:click', ['$event'])
  protected onDocClick(ev: MouseEvent): void {
    const t = ev.target as HTMLElement;
    if (!t.closest('[data-search]')) this.searchOpen.set(false);
  }
}
