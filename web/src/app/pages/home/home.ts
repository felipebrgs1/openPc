import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { httpResource } from '@angular/common/http';
import { NAV_CATEGORIES, type OffersResponse, type ProductsResponse, type StoreStats } from '../../api';
import { Seo } from '../../seo';
import { formatNumber } from '../../format';
import { CategoryIcon } from '../../components/category-icon/category-icon';
import { ProductCard } from '../../components/product-card/product-card';
import { ProductRow } from '../../components/product-row/product-row';
import { RecentProducts } from '../../recent';

@Component({
  selector: 'app-home',
  imports: [RouterLink, CategoryIcon, ProductCard, ProductRow],
  templateUrl: './home.html',
})
export class Home {
  private readonly seo = inject(Seo);
  private readonly recentStore = inject(RecentProducts);

  /** Estático no primeiro paint — sem salto quando a API responde. */
  protected readonly categories = NAV_CATEGORIES;
  protected readonly recent = this.recentStore.items;

  protected readonly stores = httpResource<StoreStats[]>(() => '/api/v1/stores');

  protected readonly offers = httpResource<OffersResponse>(
    () => '/api/v1/offers?period=7d&minPrice=1000&limit=8',
  );

  protected readonly newest = httpResource<ProductsResponse>(
    () => '/api/v1/products?sort=newest&minPrice=500&limit=8',
  );

  protected readonly formatNumber = formatNumber;

  constructor() {
    this.seo.set(
      'OpenPC — compare preços de hardware e monte seu PC',
      'Catálogo unificado de Kabum, Terabyte e Pichau com montador de PC guiado por compatibilidade.',
    );
  }
}
