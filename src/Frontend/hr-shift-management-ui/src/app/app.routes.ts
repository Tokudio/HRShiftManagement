
import { Routes } from '@angular/router';
import {
  authGuard,
  adminGuard,
  employeeGuard
} from './core/guards/auth-guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login').then(m => m.Login),
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./features/auth/register/register').then(m => m.Register),
  },
  {
    path: 'shifts',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/shifts/shifts').then(m => m.Shifts),
  },
  {
    path: 'schedule',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/schedule/schedule').then(m => m.Schedule),
  },
  {
    path: 'my-schedule',
    canActivate: [employeeGuard],
    loadComponent: () =>
      import('./features/my-schedule/my-schedule').then(m => m.MySchedule),
  },
  {
    path: 'home',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/home/home').then(m => m.Home),
  },

  // ADMIN WORKSPACE
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./features/Admin/layout/admin-layout').then(
        m => m.AdminLayout
      ),
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full',
      },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/Admin/dashboard/dashboard').then(
            m => m.Dashboard
          ),
      },
      {
        path: 'users',
        loadComponent: () =>
          import('./features/Admin/user/users').then(
            m => m.Users
          ),
      },
    ],
  },

  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full',
  },
  {
    path: '**',
    redirectTo: 'login',
  },
];
