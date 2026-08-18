import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { httpResource } from '@angular/common/http';
import { CATEGORY_LABELS, type Category as CategoryDto, type ProductsResponse } from '../../api';
import { Seo } from '../../seo';
import { ProductCard } from '../../components/product-card/product-card';
import { ProductRow } from '../../components/product-row/product-row';

interface AttrFilter {
  key: string;
  label: string;
  options: { value: string; label: string }[];
}

@Component({
  selector: 'app-category',
  imports: [RouterLink, ProductCard, ProductRow],
  templateUrl: './category.html',
})
export class Category {
  readonly category = input.required<string>();

  private readonly seo = inject(Seo);

  protected readonly query = signal('');
  protected readonly sort = signal('price_asc');
  protected readonly minPrice = signal<number | null>(null);
  protected readonly maxPrice = signal<number | null>(null);
  protected readonly limit = signal(24);
  protected readonly view = signal<'grid' | 'list'>('grid');

  /** Valor selecionado no filtro de atributo (socket, série, tipo...). */
  protected readonly attrValue = signal('');

  /** Configuração do filtro de atributo por categoria (null = sem filtro). */
  protected readonly attrFilter = computed<AttrFilter | null>(() => {
    switch (this.category()) {
      case 'cpu':
      case 'motherboard':
        return {
          key: 'socket',
          label: 'Socket',
          options: [
            { value: 'am4', label: 'AM4' },
            { value: 'am5', label: 'AM5' },
            { value: 'lga 1700', label: 'LGA 1700' },
            { value: 'lga 1851', label: 'LGA 1851' },
          ],
        };
      case 'gpu':
        return {
          key: 'series',
          label: 'Série',
          options: [
            { value: 'rtx20', label: 'RTX 20' },
            { value: 'rtx30', label: 'RTX 30' },
            { value: 'rtx40', label: 'RTX 40' },
            { value: 'rtx50', label: 'RTX 50' },
            { value: 'gtx16', label: 'GTX 16' },
            { value: 'rx5000', label: 'RX 5000' },
            { value: 'rx6000', label: 'RX 6000' },
            { value: 'rx7000', label: 'RX 7000' },
            { value: 'rx9000', label: 'RX 9000' },
            { value: 'arc', label: 'Arc' },
          ],
        };
      case 'memory':
        return {
          key: 'type',
          label: 'Tipo',
          options: [
            { value: 'ddr4', label: 'DDR4' },
            { value: 'ddr5', label: 'DDR5' },
          ],
        };
      default:
        return null;
    }
  });

  /** Chave do filtro de atributo vigente — para resetar o valor na troca de categoria. */
  private attrKey: string | null = null;

  private readonly categories = httpResource<CategoryDto[]>(() => '/api/v1/categories');

  protected readonly categoryName = computed(
    () =>
      this.categories.value()?.find((c) => c.slug === this.category())?.name ??
      CATEGORY_LABELS[this.category()] ??
      this.category(),
  );

  protected readonly products = httpResource<ProductsResponse>(() => {
    const params = new URLSearchParams({
      category: this.category(),
      sort: this.sort(),
      limit: String(this.limit()),
    });
    const q = this.query().trim();
    if (q) params.set('q', q);
    const attr = this.attrFilter();
    const attrVal = this.attrValue();
    if (attr && attrVal) params.set(`attrs[${attr.key}]`, attrVal);
    const min = this.minPrice();
    const max = this.maxPrice();
    if (min != null) params.set('minPrice', String(min));
    if (max != null) params.set('maxPrice', String(max));
    return `/api/v1/products?${params.toString()}`;
  });

  // O reload (filtro ou "Carregar mais") zera value() do httpResource — a
  // lista some e a página encolhe, jogando o scroll para o topo. lastGood
  // mantém o último valor renderizado para a lista não colapsar no reload.
  private readonly lastGood = signal<ProductsResponse | null>(null);

  protected readonly page = computed<ProductsResponse | null>(() => {
    try {
      return this.products.value() ?? this.lastGood();
    } catch {
      return this.lastGood(); // erro no reload: mostra dados anteriores
    }
  });

  constructor() {
    this.seo.set('Peças de PC');
    effect(() => this.seo.set(this.categoryName()));

    // Troca de categoria (a rota /pecas/:category reutiliza a instância):
    // reseta o valor do filtro de atributo quando a chave muda (socket →
    // série → type; o valor de uma categoria não pode vazar para outra com
    // filtro diferente — ex.: AM5 enviado como attrs[series]) e descarta o
    // lastGood da categoria anterior (senão a lista antiga pisca no load).
    effect(() => {
      const f = this.attrFilter();
      if (!f || f.key !== this.attrKey) this.attrValue.set('');
      this.attrKey = f?.key ?? null;
      this.lastGood.set(null);
    });

    effect(() => {
      try {
        const v = this.products.value();
        if (v) this.lastGood.set(v);
      } catch {
        // erro no reload: mantém o último bom
      }
    });
  }

  protected applyFilters(qEl: HTMLInputElement, minEl: HTMLInputElement, maxEl: HTMLInputElement): void {
    this.query.set(qEl.value);
    this.minPrice.set(this.parsePrice(minEl.value));
    this.maxPrice.set(this.parsePrice(maxEl.value));
    this.limit.set(24);
  }

  protected clearFilters(qEl: HTMLInputElement, minEl: HTMLInputElement, maxEl: HTMLInputElement): void {
    qEl.value = '';
    minEl.value = '';
    maxEl.value = '';
    this.query.set('');
    this.minPrice.set(null);
    this.maxPrice.set(null);
    this.sort.set('price_asc');
    this.attrValue.set('');
    this.limit.set(24);
  }

  private parsePrice(raw: string): number | null {
    const v = Number(raw.replace(',', '.'));
    return Number.isFinite(v) && v > 0 ? v : null;
  }

  protected changeSort(event: Event): void {
    this.sort.set((event.target as HTMLSelectElement).value);
  }

  protected onAttrChange(event: Event): void {
    this.attrValue.set((event.target as HTMLSelectElement).value);
    this.limit.set(24);
  }

  protected loadMore(): void {
    this.limit.update((l) => l + 24);
  }
}
