import { Injectable, inject } from '@angular/core';
import { Title } from '@angular/platform-browser';

@Injectable({ providedIn: 'root' })
export class Seo {
  private readonly title = inject(Title);

  set(title: string, description?: string): void {
    this.title.setTitle(`${title} · OpenPC`);
    if (!description) return;

    let meta = document.head.querySelector<HTMLMetaElement>('meta[name="description"]');
    if (!meta) {
      meta = document.createElement('meta');
      meta.setAttribute('name', 'description');
      document.head.appendChild(meta);
    }
    meta.setAttribute('content', description);
  }
}
