import { Component, computed, effect, inject, signal } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { NAV_CATEGORIES, categoryLabel, isMultiSlot, type BuildItemDto, type Category, type IssueDto, type ProductListItem, type ProductsResponse } from '../../api';
import { formatBRL } from '../../format';
import { Seo } from '../../seo';
import { BuildState } from '../../build-state';
import { CategoryIcon } from '../../components/category-icon/category-icon';

@Component({
  selector: 'app-builder',
  imports: [CategoryIcon],
  templateUrl: './builder.html',
})
export class Builder {
  private readonly buildState = inject(BuildState);
  private readonly seo = inject(Seo);

  protected readonly build = this.buildState.build;
  protected readonly loading = this.buildState.loading;
  protected readonly error = this.buildState.error;
  protected readonly comparison = this.buildState.comparison;

  protected readonly categories = httpResource<Category[]>(() => '/api/v1/categories');
  protected readonly slots = computed(() => {
    const fromApi = this.categories.value();
    const list = fromApi?.length ? fromApi : NAV_CATEGORIES;
    return [...list].sort((a, b) => a.displayOrder - b.displayOrder);
  });

  /** Slot aberto no seletor (modal); null = fechado. */
  protected readonly picker = signal<Category | null>(null);
  protected readonly showIncompatible = signal(false);
  protected readonly pickerQuery = signal('');
  protected readonly pickerMinPrice = signal<number | null>(null);
  protected readonly pickerMaxPrice = signal<number | null>(null);
  protected readonly pickerPage = signal(0);
  protected readonly PICKER_PAGE_SIZE = 24;

  /** Lista do seletor: filtrada pela engine (compatibleWith) ou com motivos (showIncompatible). */
  protected readonly pickerProducts = httpResource<ProductsResponse>(() => {
    const cat = this.picker();
    const slug = this.buildState.slug();
    if (!cat || !slug) return undefined;
    const q = this.pickerQuery().trim();
    const params = new URLSearchParams({
      category: cat.slug,
      compatibleWith: slug,
      showIncompatible: String(this.showIncompatible()),
      sort: 'price_asc',
      limit: String(this.PICKER_PAGE_SIZE),
      offset: String(this.pickerPage() * this.PICKER_PAGE_SIZE),
    });
    if (q) params.set('q', q);
    const min = this.pickerMinPrice();
    const max = this.pickerMaxPrice();
    if (min != null) params.set('minPrice', String(min));
    if (max != null) params.set('maxPrice', String(max));
    return `/api/v1/products?${params.toString()}`;
  });

  // O reload (troca de página ou filtro) zera value() do httpResource e a
  // lista some do modal. lastGood mantém o último resultado renderizado para
  // a lista não colapsar enquanto a próxima página carrega.
  private readonly pickerLastGood = signal<ProductsResponse | null>(null);

  protected readonly pickerPageData = computed<ProductsResponse | null>(() => {
    try {
      return this.pickerProducts.value() ?? this.pickerLastGood();
    } catch {
      return this.pickerLastGood(); // erro no reload: mostra dados anteriores
    }
  });

  protected readonly pickerTotalPages = computed(() => {
    const total = this.pickerPageData()?.total ?? 0;
    return Math.max(1, Math.ceil(total / this.PICKER_PAGE_SIZE));
  });

  protected readonly itemsByCategory = computed(() => {
    const map = new Map<string, BuildItemDto[]>();
    for (const item of this.build()?.items ?? []) {
      const list = map.get(item.category) ?? [];
      list.push(item);
      map.set(item.category, list);
    }
    return map;
  });

  protected readonly wattagePct = computed(() => {
    const w = this.build()?.wattage;
    if (!w || !w.known || w.recommendedW <= 0) return 0;
    return Math.min(100, Math.round((w.baseW / w.recommendedW) * 100));
  });

  protected copied = signal(false);
  protected readonly origin = location.origin;
  protected readonly formatBRL = formatBRL;
  protected readonly categoryLabel = categoryLabel;
  protected readonly isMultiSlot = isMultiSlot;

  constructor() {
    this.seo.set(
      'Montar meu PC',
      'Monte um PC completo com filtro de compatibilidade automático entre Kabum, Terabyte e Pichau.',
    );

    // Troca de filtro (busca, faixa de preço, incompatíveis) ou de categoria
    // volta o seletor para a primeira página.
    effect(() => {
      this.picker();
      this.pickerQuery();
      this.pickerMinPrice();
      this.pickerMaxPrice();
      this.showIncompatible();
      this.pickerPage.set(0);
    });

    effect(() => {
      try {
        const v = this.pickerProducts.value();
        if (v) this.pickerLastGood.set(v);
      } catch {
        // erro no reload: mantém o último bom
      }
    });

    void this.init();
  }

  private async init(): Promise<void> {
    try {
      await this.buildState.ensure();
      await this.buildState.refresh();
    } catch {
      this.error.set('Não foi possível iniciar o build.');
    }
  }

  protected openPicker(cat: Category): void {
    this.picker.set(cat);
    this.showIncompatible.set(false);
    this.pickerQuery.set('');
    this.pickerMinPrice.set(null);
    this.pickerMaxPrice.set(null);
    this.pickerPage.set(0);
    this.pickerLastGood.set(null); // não deixa a lista da categoria anterior piscar
  }

  protected closePicker(): void {
    this.picker.set(null);
  }

  protected onPickerSearch(event: Event): void {
    this.pickerQuery.set((event.target as HTMLInputElement).value);
  }

  protected applyPickerPriceFilter(minEl: HTMLInputElement, maxEl: HTMLInputElement): void {
    this.pickerMinPrice.set(this.parsePrice(minEl.value));
    this.pickerMaxPrice.set(this.parsePrice(maxEl.value));
  }

  protected clearPickerPriceFilter(minEl: HTMLInputElement, maxEl: HTMLInputElement): void {
    minEl.value = '';
    maxEl.value = '';
    this.pickerMinPrice.set(null);
    this.pickerMaxPrice.set(null);
  }

  private parsePrice(raw: string): number | null {
    const v = Number(raw.replace(',', '.'));
    return Number.isFinite(v) && v > 0 ? v : null;
  }

  protected nextPickerPage(): void {
    if (this.pickerPage() + 1 < this.pickerTotalPages()) {
      this.pickerPage.update((p) => p + 1);
    }
  }

  protected prevPickerPage(): void {
    this.pickerPage.update((p) => Math.max(0, p - 1));
  }

  protected async select(cat: Category, product: ProductListItem): Promise<void> {
    this.error.set(null);
    try {
      await this.buildState.chooseItem(cat.slug, product.id);
      this.picker.set(null);
    } catch {
      this.error.set('Falha ao adicionar a peça ao build.');
    }
  }

  protected async remove(cat: Category): Promise<void> {
    this.error.set(null);
    try {
      await this.buildState.removeItem(cat.slug);
    } catch {
      this.error.set('Falha ao remover a peça.');
    }
  }

  protected async removeOne(item: BuildItemDto): Promise<void> {
    this.error.set(null);
    try {
      await this.buildState.removeItemById(item.id);
    } catch {
      this.error.set('Falha ao remover a peça.');
    }
  }

  protected issueProductNames(issue: IssueDto): string[] {
    const items = this.build()?.items ?? [];
    return issue.products
      .map((id) => items.find((i) => i.productId === id)?.name)
      .filter((name): name is string => !!name);
  }

  protected async copyLink(): Promise<void> {
    const slug = this.buildState.slug();
    if (!slug) return;
    const url = `${location.origin}/build/${slug}`;
    const ok = await copyText(url);
    if (!ok) return; // clipboard indisponível — sem ação
    this.copied.set(true);
    setTimeout(() => this.copied.set(false), 1500);
  }
}

/**
 * Copia texto para a área de transferência com fallback para navegadores
 * sem Clipboard API (http fora de localhost, iframes sem permissão).
 */
async function copyText(text: string): Promise<boolean> {
  if (navigator.clipboard?.writeText) {
    try {
      await navigator.clipboard.writeText(text);
      return true;
    } catch {
      /* cai no fallback */
    }
  }
  try {
    const ta = document.createElement('textarea');
    ta.value = text;
    ta.style.position = 'fixed';
    ta.style.opacity = '0';
    document.body.appendChild(ta);
    ta.focus();
    ta.select();
    const ok = document.execCommand('copy');
    ta.remove();
    return ok;
  } catch {
    return false;
  }
}
