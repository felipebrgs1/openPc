import { Component, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { httpResource } from '@angular/common/http';
import type { Category as CategoryDto, ProductsResponse } from '../api';
import { Seo } from '../seo';
import { ProductCard } from '../components/product-card';

@Component({
  selector: 'app-category',
  imports: [RouterLink, ProductCard],
  templateUrl: './category.html',
})
export class Category {
  readonly category = input.required<string>();

  private readonly seo = inject(Seo);

  protected readonly query = signal('');
  protected readonly sort = signal('price_asc');
  protected readonly limit = signal(24);

  private readonly categories = httpResource<CategoryDto[]>(() => '/api/v1/categories');

  protected readonly categoryName = computed(
    () =>
      this.categories.value()?.find((c) => c.slug === this.category())?.name ??
      this.category(),
  );

  protected readonly products = httpResource<ProductsResponse>(() => {
    const q = this.query().trim();
    const params = new URLSearchParams({
      category: this.category(),
      sort: this.sort(),
      limit: String(this.limit()),
    });
    if (q) params.set('q', q);
    return `/api/v1/products?${params.toString()}`;
  });

  constructor() {
    this.seo.set('Peças de PC');
  }

  protected applyQuery(event: Event): void {
    this.query.set((event.target as HTMLInputElement).value);
    this.limit.set(24);
  }

  protected changeSort(event: Event): void {
    this.sort.set((event.target as HTMLSelectElement).value);
  }

  protected loadMore(): void {
    this.limit.update((l) => l + 24);
  }
}
