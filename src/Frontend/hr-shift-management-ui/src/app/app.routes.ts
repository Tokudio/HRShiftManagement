import { Routes } from '@angular/router';
import {
  authGuard,
  adminGuard
} from './core/guards/auth-guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login').then(
        (m) => m.Login
      ),
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./features/auth/register/register').then(
        (m) => m.Register
      ),
  },
  {
    // US 2.1 - Quan ly ca lam viec
    path: 'shifts',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/shifts/shifts').then(
        (m) => m.Shifts
      ),
  },
  {
    // US 2.2 - Lich phan ca nhan vien
    path: 'schedule',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/schedule/schedule').then(
        (m) => m.Schedule
      ),
  },
  {
    path: 'home',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/home/home').then(
        (m) => m.Home
      ),
  },
  {
    // Trang quan ly nguoi dung - Admin
    path: 'admin/users',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./features/Admin/user/users').then(
        (m) => m.Users
      ),
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
