import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { httpResource } from '@angular/common/http';
import type { Category } from '../api';
import { Seo } from '../seo';

@Component({
  selector: 'app-home',
  imports: [RouterLink],
  templateUrl: './home.html',
})
export class Home {
  private readonly seo = inject(Seo);

  protected readonly categories = httpResource<Category[]>(() => '/api/v1/categories');

  constructor() {
    this.seo.set(
      'OpenPC — compare preços de hardware e monte seu PC',
      'Catálogo unificado de Kabum, Terabyte e Pichau com montador de PC guiado por compatibilidade.',
    );
  }
}
