import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import type { ProductListItem } from '../../api';
import { formatBRL } from '../../format';

@Component({
  selector: 'app-product-card',
  imports: [RouterLink],
  templateUrl: './product-card.html',
})
export class ProductCard {
  readonly product = input.required<ProductListItem>();
  readonly price = input<number | null>(null);
  readonly oldPrice = input<number | null>(null);
  readonly dropPercent = input<number | null>(null);
  readonly lowestInDays = input(0);

  protected readonly formatBRL = formatBRL;

  protected readonly displayPrice = computed(() => this.price() ?? this.product().price);
}
