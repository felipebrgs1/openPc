import { Component, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { httpResource } from '@angular/common/http';
import type { BuildDto, PriceComparison } from '../../api';
import { categoryLabel } from '../../api';
import { formatBRL, formatDateTime } from '../../format';
import { Seo } from '../../seo';
import { BuildState } from '../../build-state';
import { CategoryIcon } from '../../components/category-icon/category-icon';

@Component({
  selector: 'app-build-view',
  imports: [RouterLink, CategoryIcon],
  templateUrl: './build-view.html',
})
export class BuildView {
  readonly slug = input.required<string>();

  private readonly seo = inject(Seo);
  private readonly buildState = inject(BuildState);
  private readonly router = inject(Router);

  protected readonly build = httpResource<BuildDto>(() => `/api/v1/builds/${this.slug()}`);
  protected readonly comparison = httpResource<PriceComparison>(() =>
    `/api/v1/builds/${this.slug()}/price-comparison`,
  );

  protected cloning = signal(false);
  protected readonly formatBRL = formatBRL;
  protected readonly formatDateTime = formatDateTime;
  protected readonly categoryLabel = categoryLabel;

  constructor() {
    this.seo.set('Build compartilhado');
  }

  protected async clone(): Promise<void> {
    this.cloning.set(true);
    try {
      await this.buildState.clone(this.slug());
      await this.router.navigate(['/montar']);
    } finally {
      this.cloning.set(false);
    }
  }
}
