import { Injectable, signal } from '@angular/core';
import type { ProductListItem } from './api';

const LS_KEY = 'openpc.recent';
const MAX = 8;

/**
 * Últimos produtos visitados (localStorage). Instantâneo no primeiro paint —
 * não depende da API, então a seção na home não empurra o layout.
 */
@Injectable({ providedIn: 'root' })
export class RecentProducts {
  readonly items = signal<ProductListItem[]>(load());

  push(product: ProductListItem): void {
    const next = [product, ...this.items().filter((p) => p.id !== product.id)].slice(0, MAX);
    this.items.set(next);
    try {
      localStorage.setItem(LS_KEY, JSON.stringify(next));
    } catch {
      /* quota / modo privado */
    }
  }
}

function load(): ProductListItem[] {
  try {
    const raw = localStorage.getItem(LS_KEY);
    return raw ? (JSON.parse(raw) as ProductListItem[]) : [];
  } catch {
    return [];
  }
}
