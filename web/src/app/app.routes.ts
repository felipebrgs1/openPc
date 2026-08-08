import { Routes } from '@angular/router';
import { Home } from './pages/home';
import { Category } from './pages/category';
import { Product } from './pages/product';
import { Builder } from './pages/builder';
import { BuildView } from './pages/build-view';
import { Offers } from './pages/offers';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'pecas/:category', component: Category },
  { path: 'pecas/:category/:id', component: Product },
  { path: 'montar', component: Builder },
  { path: 'build/:slug', component: BuildView },
  { path: 'ofertas', component: Offers },
  { path: '**', redirectTo: '' },
];
