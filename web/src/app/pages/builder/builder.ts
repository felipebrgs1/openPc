import { Component, computed, inject, signal } from '@angular/core';
import { httpResource } from '@angular/common/http';
import type { BuildItemDto, Category, IssueDto, ProductListItem, ProductsResponse } from '../../api';
import { categoryLabel, isMultiSlot } from '../../api';
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
  protected readonly slots = computed(() =>
    [...(this.categories.value() ?? [])].sort((a, b) => a.displayOrder - b.displayOrder),
  );

  /** Slot aberto no seletor (modal); null = fechado. */
  protected readonly picker = signal<Category | null>(null);
  protected readonly showIncompatible = signal(false);
  protected readonly pickerQuery = signal('');

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
      limit: '100',
    });
    if (q) params.set('q', q);
    return `/api/v1/products?${params.toString()}`;
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
  }

  protected closePicker(): void {
    this.picker.set(null);
  }

  protected onPickerSearch(event: Event): void {
    this.pickerQuery.set((event.target as HTMLInputElement).value);
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
