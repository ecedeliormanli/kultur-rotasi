import { Routes } from '@angular/router';

export const routes: Routes = [
  // Lazy loaded: Leaflet only downloads when the map page opens
  { path: '', loadComponent: () => import('./features/map/map-page').then((m) => m.MapPage) },
  { path: '**', redirectTo: '' },
];
