import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { httpResource } from '@angular/common/http';
import type { OffersResponse } from '../api';
import { formatBRL } from '../format';
import { Seo } from '../seo';

@Component({
  selector: 'app-offers',
  imports: [RouterLink],
  templateUrl: './offers.html',
})
export class Offers {
  private readonly seo = inject(Seo);

  protected readonly period = signal<'24h' | '7d'>('7d');
  protected readonly offers = httpResource<OffersResponse>(
    () => `/api/v1/offers?period=${this.period()}&limit=30`,
  );

  protected readonly formatBRL = formatBRL;

  constructor() {
    this.seo.set(
      'OpenPC — ofertas e quedas de preço',
      'Maiores quedas de preço em hardware nas últimas 24 horas e 7 dias.',
    );
  }

  protected dropPercent(item: OffersResponse['items'][number]): number | null {
    return this.period() === '24h' ? item.dropPercent24h : item.dropPercent7d;
  }

  protected switchPeriod(p: '24h' | '7d'): void {
    this.period.set(p);
  }
}
