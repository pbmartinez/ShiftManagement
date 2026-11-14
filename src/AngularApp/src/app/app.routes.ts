import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'shifts', pathMatch: 'full' },
  { path: 'shifts', loadComponent: () => import('./shifts/shifts').then(m => m.Shifts) },
  { path: 'collaborators', loadComponent: () => import('./collaborators/collaborators').then(m => m.Collaborators) },
  { path: '**', redirectTo: 'shifts' },
];
