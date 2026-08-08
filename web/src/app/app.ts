import { Component, computed } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { httpResource } from '@angular/common/http';
import type { Category } from './api';
import { CategoryIcon } from './components/category-icon';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CategoryIcon],
  templateUrl: './app.html',
})
export class App {
  private readonly categories = httpResource<Category[]>(() => '/api/v1/categories');

  protected readonly navCategories = computed(() =>
    [...(this.categories.value() ?? [])].sort((a, b) => a.displayOrder - b.displayOrder),
  );
}
