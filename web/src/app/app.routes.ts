import { Routes } from '@angular/router';
import { Home } from './pages/home/home';
import { Category } from './pages/category/category';
import { Product } from './pages/product/product';
import { Builder } from './pages/builder/builder';
import { BuildView } from './pages/build-view/build-view';
import { Offers } from './pages/offers/offers';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'pecas/:category', component: Category },
  { path: 'pecas/:category/:id', component: Product },
  { path: 'montar', component: Builder },
  { path: 'build/:slug', component: BuildView },
  { path: 'ofertas', component: Offers },
  { path: '**', redirectTo: '' },
];
