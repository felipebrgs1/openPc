import { Component, computed, input } from '@angular/core';
import type { PricePoint } from '../../api';
import { formatBRL, formatDate } from '../../format';

const W = 640;
const H = 200;
const PAD = { top: 16, right: 12, bottom: 28, left: 12 };

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

    const plotW = W - PAD.left - PAD.right;
    const plotH = H - PAD.top - PAD.bottom;
    const x = (i: number) => PAD.left + (i * plotW) / (pts.length - 1);
    const y = (v: number) => PAD.top + plotH - ((v - min) * plotH) / span;

    const line = pts
      .map((p, i) => `${i === 0 ? 'M' : 'L'}${x(i).toFixed(1)},${y(p.price).toFixed(1)}`)
      .join(' ');
    const area = `${line} L${x(pts.length - 1).toFixed(1)},${PAD.top + plotH} L${x(0).toFixed(1)},${PAD.top + plotH} Z`;

    // labels de data: primeira, meio e última
    const labelIdx = [0, Math.floor((pts.length - 1) / 2), pts.length - 1];
    const labels = labelIdx.map((i) => ({
      text: formatDate(pts[i].date),
      x: x(i),
    }));

    // linha de referência do preço atual
    const lastPrice = pts[pts.length - 1].price;

    return {
      line,
      area,
      last: pts[pts.length - 1],
      min,
      max,
      labels,
      lastY: y(lastPrice),
      width: W,
      height: H,
    };
  });
}
