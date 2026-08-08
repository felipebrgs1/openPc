import { Component, computed, input } from '@angular/core';
import type { PricePoint } from '../api';
import { formatBRL, formatDate } from '../format';

const W = 320;
const H = 72;
const PAD = 4;

@Component({
  selector: 'app-sparkline',
  templateUrl: './sparkline.html',
})
export class Sparkline {
  readonly points = input.required<PricePoint[]>();
  protected readonly formatBRL = formatBRL;
  protected readonly formatDate = formatDate;

  protected readonly geometry = computed(() => {
    const pts = this.points();
    if (pts.length < 2) return null;

    const prices = pts.map((p) => p.price);
    const min = Math.min(...prices);
    const max = Math.max(...prices);
    const span = max - min || 1;

    const x = (i: number) => PAD + (i * (W - 2 * PAD)) / (pts.length - 1);
    const y = (v: number) => H - PAD - ((v - min) * (H - 2 * PAD)) / span;

    const line = pts.map((p, i) => `${i === 0 ? 'M' : 'L'}${x(i).toFixed(1)},${y(p.price).toFixed(1)}`).join(' ');
    const area = `${line} L${x(pts.length - 1).toFixed(1)},${H - PAD} L${x(0).toFixed(1)},${H - PAD} Z`;

    return {
      line,
      area,
      last: pts[pts.length - 1],
      min,
      max,
      width: W,
      height: H,
    };
  });
}
