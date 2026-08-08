import { Component, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { httpResource } from '@angular/common/http';
import { HttpClient } from '@angular/common/http';
import type { PricePoint, ProductDetail } from '../api';
import { formatBRL, formatDateTime, formatSpecValue, specLabel } from '../format';
import { Seo } from '../seo';
import { BuildState } from '../build-state';
import { Sparkline } from '../components/sparkline';

@Component({
  selector: 'app-product',
  imports: [RouterLink, Sparkline],
  templateUrl: './product.html',
})
export class Product {
  readonly category = input.required<string>();
  readonly id = input.required<string>();

  private readonly http = inject(HttpClient);
  private readonly seo = inject(Seo);
  private readonly buildState = inject(BuildState);

  protected readonly product = httpResource<ProductDetail>(() => `/api/v1/products/${this.id()}`);
  protected readonly prices = httpResource<PricePoint[]>(() => `/api/v1/products/${this.id()}/prices?days=90`);

  protected readonly bestPrice = computed(() => {
    const listings = this.product.value()?.listings ?? [];
    const inStock = listings.filter((l) => l.inStock && l.priceCash != null);
    return inStock.length ? Math.min(...inStock.map((l) => l.priceCash!)) : null;
  });

  protected adding = signal(false);
  protected added = signal(false);

  protected readonly formatBRL = formatBRL;
  protected readonly formatDateTime = formatDateTime;
  protected readonly formatSpecValue = formatSpecValue;
  protected readonly specLabel = specLabel;

  constructor() {
    this.seo.set('Produto');
  }

  async addToBuild(): Promise<void> {
    this.adding.set(true);
    try {
      await this.buildState.setItem(this.category(), this.id());
      this.added.set(true);
    } finally {
      this.adding.set(false);
    }
  }
}
