import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { httpResource } from '@angular/common/http';
import type { Category, StoreStats } from '../../api';
import { Seo } from '../../seo';
import { formatNumber } from '../../format';
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

  protected readonly formatNumber = formatNumber;

  constructor() {
    this.seo.set(
      'OpenPC — compare preços de hardware e monte seu PC',
      'Catálogo unificado de Kabum, Terabyte e Pichau com montador de PC guiado por compatibilidade.',
    );
  }
}
