import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import type { ProductListItem } from '../api';
import { formatBRL } from '../format';

@Component({
  selector: 'app-product-card',
  imports: [RouterLink],
  templateUrl: './product-card.html',
})
export class ProductCard {
  readonly product = input.required<ProductListItem>();
  protected readonly formatBRL = formatBRL;
}
