import { Component } from '@angular/core';
import { httpResource } from '@angular/common/http';

export interface Category {
  id: string;
  slug: string;
  name: string;
  displayOrder: number;
}

@Component({
  selector: 'app-categories',
  templateUrl: './categories.html',
  styleUrl: './categories.css',
})
export class Categories {
  protected readonly categories = httpResource<Category[]>(() => '/api/v1/categories');
}
