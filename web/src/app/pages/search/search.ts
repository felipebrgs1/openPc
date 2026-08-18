import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { httpResource } from '@angular/common/http';
import { map } from 'rxjs';
import type { ProductsResponse } from '../../api';
import { Seo } from '../../seo';
import { ProductCard } from '../../components/product-card/product-card';

@Component({
  selector: 'app-search',
  imports: [RouterLink, ProductCard],
  templateUrl: './search.html',
})
export class Search {
  private readonly seo = inject(Seo);
  private readonly route = inject(ActivatedRoute);

  protected readonly q = toSignal(
    this.route.queryParamMap.pipe(map((p) => p.get('q')?.trim() ?? '')),
    { initialValue: '' },
  );

  protected readonly results = httpResource<ProductsResponse>(() => {
    const q = this.q();
    if (!q) return undefined;
    return `/api/v1/products?q=${encodeURIComponent(q)}&limit=48&sort=price_asc`;
  });

  constructor() {
    this.seo.set('Busca', 'Busque peças de PC no catálogo unificado do OpenPC.');
  }
}
