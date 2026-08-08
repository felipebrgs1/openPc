import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { httpResource } from '@angular/common/http';
import type { Category as CategoryDto, ProductsResponse } from '../../api';
import { Seo } from '../../seo';
import { formatBRL } from '../../format';

@Component({
  selector: 'app-category',
  imports: [RouterLink],
  templateUrl: './category.html',
})
export class Category {
  readonly category = input.required<string>();

  private readonly seo = inject(Seo);

  protected readonly query = signal('');
  protected readonly sort = signal('price_asc');
  protected readonly socket = signal('');
  protected readonly minPrice = signal<number | null>(null);
  protected readonly maxPrice = signal<number | null>(null);
  protected readonly limit = signal(24);

  private readonly categories = httpResource<CategoryDto[]>(() => '/api/v1/categories');

  protected readonly categoryName = computed(
    () =>
      this.categories.value()?.find((c) => c.slug === this.category())?.name ??
      this.category(),
  );

  protected readonly products = httpResource<ProductsResponse>(() => {
    const params = new URLSearchParams({
      category: this.category(),
      sort: this.sort(),
      limit: String(this.limit()),
    });
    const q = this.query().trim();
    if (q) params.set('q', q);
    const socket = this.socket();
    if (socket) params.set('attrs[socket]', socket);
    const min = this.minPrice();
    const max = this.maxPrice();
    if (min != null) params.set('minPrice', String(min));
    if (max != null) params.set('maxPrice', String(max));
    return `/api/v1/products?${params.toString()}`;
  });

  // O reload (filtro ou "Carregar mais") zera value() do httpResource — a
  // lista some e a página encolhe, jogando o scroll para o topo. lastGood
  // mantém o último valor renderizado para a lista não colapsar no reload.
  private readonly lastGood = signal<ProductsResponse | null>(null);

  protected readonly page = computed<ProductsResponse | null>(() => {
    try {
      return this.products.value() ?? this.lastGood();
    } catch {
      return this.lastGood(); // erro no reload: mostra dados anteriores
    }
  });

  protected readonly formatBRL = formatBRL;

  constructor() {
    this.seo.set('Peças de PC');
    effect(() => {
      try {
        const v = this.products.value();
        if (v) this.lastGood.set(v);
      } catch {
        // erro no reload: mantém o último bom
      }
    });
  }

  protected applyFilters(qEl: HTMLInputElement, minEl: HTMLInputElement, maxEl: HTMLInputElement): void {
    this.query.set(qEl.value);
    this.minPrice.set(this.parsePrice(minEl.value));
    this.maxPrice.set(this.parsePrice(maxEl.value));
    this.limit.set(24);
  }

  protected clearFilters(qEl: HTMLInputElement, minEl: HTMLInputElement, maxEl: HTMLInputElement): void {
    qEl.value = '';
    minEl.value = '';
    maxEl.value = '';
    this.query.set('');
    this.minPrice.set(null);
    this.maxPrice.set(null);
    this.sort.set('price_asc');
    this.socket.set('');
    this.limit.set(24);
  }

  private parsePrice(raw: string): number | null {
    const v = Number(raw.replace(',', '.'));
    return Number.isFinite(v) && v > 0 ? v : null;
  }

  protected changeSort(event: Event): void {
    this.sort.set((event.target as HTMLSelectElement).value);
  }

  protected onSocketChange(event: Event): void {
    this.socket.set((event.target as HTMLSelectElement).value);
    this.limit.set(24);
  }

  protected loadMore(): void {
    this.limit.update((l) => l + 24);
  }
}
