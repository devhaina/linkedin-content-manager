import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'campaigns' },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register/register').then((m) => m.Register),
  },
  {
    path: 'campaigns',
    canActivate: [authGuard],
    loadComponent: () => import('./features/campaigns/campaign-list/campaign-list').then((m) => m.CampaignList),
  },
  {
    path: 'campaigns/new',
    canActivate: [authGuard],
    loadComponent: () => import('./features/campaigns/campaign-form/campaign-form').then((m) => m.CampaignForm),
  },
  {
    path: 'campaigns/:id/edit',
    canActivate: [authGuard],
    loadComponent: () => import('./features/campaigns/campaign-form/campaign-form').then((m) => m.CampaignForm),
  },
  {
    path: 'content',
    canActivate: [authGuard],
    loadComponent: () => import('./features/content/content-history/content-history').then((m) => m.ContentHistory),
  },
  {
    path: 'content/new',
    canActivate: [authGuard],
    loadComponent: () => import('./features/content/content-generate/content-generate').then((m) => m.ContentGenerate),
  },
  {
    path: 'content/:id',
    canActivate: [authGuard],
    loadComponent: () => import('./features/content/content-detail/content-detail').then((m) => m.ContentDetail),
  },
  {
    path: 'schedules',
    canActivate: [authGuard],
    loadComponent: () => import('./features/schedules/schedule-list/schedule-list').then((m) => m.ScheduleList),
  },
  { path: '**', redirectTo: 'campaigns' },
];
