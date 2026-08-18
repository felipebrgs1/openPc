import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { httpResource } from '@angular/common/http';
import { HttpClient } from '@angular/common/http';
import { categoryLabel, type AlertResponse, type PricePoint, type ProductDetail } from '../../api';
import { formatBRL, formatDateTime, formatSpecValue, specGroups, specLabel } from '../../format';
import { Seo } from '../../seo';
import { BuildState } from '../../build-state';
import { RecentProducts } from '../../recent';
import { Sparkline } from '../../components/sparkline/sparkline';

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
  private readonly recent = inject(RecentProducts);

  protected readonly product = httpResource<ProductDetail>(() => `/api/v1/products/${this.id()}`);
  protected readonly prices = httpResource<PricePoint[]>(() => `/api/v1/products/${this.id()}/prices?days=90`);

  protected readonly bestPrice = computed(() => {
    const listings = this.product.value()?.listings ?? [];
    const inStock = listings.filter((l) => l.inStock && l.priceCash != null);
    return inStock.length ? Math.min(...inStock.map((l) => l.priceCash!)) : null;
  });

  protected adding = signal(false);
  protected added = signal(false);

  // alerta de preço (M6)
  protected alertEmail = signal('');
  protected alertTarget = signal<string>('');
  protected alertSending = signal(false);
  protected alertSent = signal<AlertResponse | null>(null);
  protected alertError = signal('');

  protected readonly formatBRL = formatBRL;
  protected readonly formatDateTime = formatDateTime;
  protected readonly formatSpecValue = formatSpecValue;
  protected readonly specLabel = specLabel;
  protected readonly specGroups = specGroups;
  protected readonly categoryLabel = categoryLabel;

  constructor() {
    this.seo.set('Produto');

    effect(() => {
      const p = this.product.value();
      if (!p) return;
      this.seo.set(p.name, `${p.brand} no OpenPC — compare preços e adicione ao build.`);
      this.recent.push({
        id: p.id,
        name: p.name,
        brand: p.brand,
        model: p.model,
        partNumber: p.partNumber,
        imageUrl: p.imageUrl,
        categorySlug: p.categorySlug,
        price: this.bestPrice(),
        storeCount: p.listings.length,
      });
    });
  }

  async addToBuild(): Promise<void> {
    this.adding.set(true);
    try {
      // memory/storage: adiciona mais uma peça; demais: substitui o slot
      await this.buildState.chooseItem(this.category(), this.id());
      this.added.set(true);
    } finally {
      this.adding.set(false);
    }
  }

  async createAlert(): Promise<void> {
    const target = Number(this.alertTarget().replace(',', '.'));
    if (!this.alertEmail().includes('@') || !target || target <= 0) {
      this.alertError.set('Informe um e-mail válido e um preço alvo.');
      return;
    }

    this.alertSending.set(true);
    this.alertError.set('');
    try {
      const alert = await this.http
        .post<AlertResponse>('/api/v1/alerts', {
          productId: this.id(),
          email: this.alertEmail(),
          targetPrice: target,
        })
        .toPromise();
      this.alertSent.set(alert!);
    } catch {
      this.alertError.set('Não foi possível criar o alerta. Tente novamente.');
    } finally {
      this.alertSending.set(false);
    }
  }
}
