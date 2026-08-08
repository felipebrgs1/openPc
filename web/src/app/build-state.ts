import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import type { BuildDto, PriceComparison } from './api';

const LS_KEY = 'openpc.build.slug';

/**
 * Estado do build atual (draft anônimo): o slug vive no localStorage e o
 * build em si é criado/servido pela API (specs.md §7: persistência em
 * localStorage + estado com signals).
 */
@Injectable({ providedIn: 'root' })
export class BuildState {
  private readonly http = inject(HttpClient);

  readonly slug = signal<string | null>(null);
  readonly build = signal<BuildDto | null>(null);
  readonly comparison = signal<PriceComparison | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  constructor() {
    const saved = localStorage.getItem(LS_KEY);
    if (saved) {
      this.slug.set(saved);
      void this.refresh();
    }
  }

  /** Garante um build existente (cria se não houver slug) e devolve o slug. */
  async ensure(): Promise<string> {
    const current = this.slug();
    if (current) return current;

    const created = await firstValueFrom(
      this.http.post<{ slug: string }>('/api/v1/builds', { name: 'Meu build' }),
    );
    this.slug.set(created.slug);
    localStorage.setItem(LS_KEY, created.slug);
    return created.slug;
  }

  async refresh(): Promise<void> {
    const slug = this.slug();
    if (!slug) return;

    this.loading.set(true);
    try {
      this.build.set(await firstValueFrom(this.http.get<BuildDto>(`/api/v1/builds/${slug}`)));
      this.comparison.set(
        await firstValueFrom(this.http.get<PriceComparison>(`/api/v1/builds/${slug}/price-comparison`)),
      );
      this.error.set(null);
    } catch {
      this.error.set('Não foi possível carregar o build.');
    } finally {
      this.loading.set(false);
    }
  }

  async setItem(category: string, productId: string): Promise<void> {
    const slug = await this.ensure();
    await firstValueFrom(this.http.put(`/api/v1/builds/${slug}/items/${category}`, { productId }));
    await this.refresh();
  }

  async removeItem(category: string): Promise<void> {
    const slug = this.slug();
    if (!slug) return;
    await firstValueFrom(this.http.delete(`/api/v1/builds/${slug}/items/${category}`));
    await this.refresh();
  }

  /** Copia um build público para o draft atual e devolve o novo slug. */
  async clone(sourceSlug: string): Promise<string> {
    const source = await firstValueFrom(this.http.get<BuildDto>(`/api/v1/builds/${sourceSlug}`));
    const created = await firstValueFrom(
      this.http.post<{ slug: string }>('/api/v1/builds', { name: `${source.name} (cópia)` }),
    );

    for (const item of source.items) {
      if (!item.productId) continue;
      await firstValueFrom(
        this.http.put(`/api/v1/builds/${created.slug}/items/${item.category}`, { productId: item.productId }),
      );
    }

    this.slug.set(created.slug);
    localStorage.setItem(LS_KEY, created.slug);
    await this.refresh();
    return created.slug;
  }
}
