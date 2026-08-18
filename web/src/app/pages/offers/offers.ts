import { Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { httpResource } from '@angular/common/http';
import type { OffersResponse } from '../../api';
import { Seo } from '../../seo';
import { ProductCard } from '../../components/product-card/product-card';

@Component({
  selector: 'app-offers',
  imports: [RouterLink, ProductCard],
  templateUrl: './offers.html',
})
export class Offers {
  private readonly seo = inject(Seo);

  protected readonly period = signal<'24h' | '7d'>('7d');
  protected readonly offers = httpResource<OffersResponse>(
    () => `/api/v1/offers?period=${this.period()}&limit=30`,
  );

  private readonly lastGood = signal<OffersResponse | null>(null);

  protected readonly page = computed<OffersResponse | null>(() => {
    try {
      return this.offers.value() ?? this.lastGood();
    } catch {
      return this.lastGood();
    }
  });

  constructor() {
    this.seo.set(
      'OpenPC — ofertas e quedas de preço',
      'Maiores quedas de preço em hardware nas últimas 24 horas e 7 dias.',
    );

    effect(() => {
      try {
        const v = this.offers.value();
        if (v) this.lastGood.set(v);
      } catch {
        /* mantém o último bom */
      }
    });
  }

  protected dropPercent(item: OffersResponse['items'][number]): number | null {
    return this.period() === '24h' ? item.dropPercent24h : item.dropPercent7d;
  }

  protected switchPeriod(p: '24h' | '7d'): void {
    this.period.set(p);
  }
}
