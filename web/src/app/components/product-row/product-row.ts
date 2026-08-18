import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import type { ProductListItem } from '../../api';
import { formatBRL } from '../../format';

@Component({
  selector: 'app-product-row',
  imports: [RouterLink],
  templateUrl: './product-row.html',
})
export class ProductRow {
  readonly product = input.required<ProductListItem>();
  protected readonly formatBRL = formatBRL;
}
