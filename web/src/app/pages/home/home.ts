import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { httpResource } from '@angular/common/http';
import type { Category, OffersResponse, ProductsResponse, StoreStats } from '../../api';
import { Seo } from '../../seo';
import { formatBRL, formatNumber } from '../../format';
import { CategoryIcon } from '../../components/category-icon/category-icon';

@Component({
  selector: 'app-home',
  imports: [RouterLink, CategoryIcon],
  templateUrl: './home.html',
})
export class Home {
  private readonly seo = inject(Seo);

  protected readonly categories = httpResource<Category[]>(() => '/api/v1/categories');

  /** Banner da home: quantidade de itens com valor por loja. */
  protected readonly stores = httpResource<StoreStats[]>(() => '/api/v1/stores');

  /** Ofertas: quedas de preço em produtos acima de R$ 1.000. */
  protected readonly offers = httpResource<OffersResponse>(
    () => '/api/v1/offers?period=7d&minPrice=1000&limit=8',
  );

  /** Últimos produtos adicionados ao catálogo (acima de R$ 500). */
  protected readonly newest = httpResource<ProductsResponse>(
    () => '/api/v1/products?sort=newest&minPrice=500&limit=8',
  );

  protected readonly formatNumber = formatNumber;
  protected readonly formatBRL = formatBRL;

  constructor() {
    this.seo.set(
      'OpenPC — compare preços de hardware e monte seu PC',
      'Catálogo unificado de Kabum, Terabyte e Pichau com montador de PC guiado por compatibilidade.',
    );
  }
}
