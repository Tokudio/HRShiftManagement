import { authGuard } from './core/guards/auth-guard';
import { Routes } from '@angular/router';

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
        // Trang quan ly ca lam viec - US 2.1
        path: 'shifts',
        canActivate: [authGuard],
        loadComponent: () =>
            import('./features/shifts/shifts').then(
                (m) => m.Shifts
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
        path: '',
        redirectTo: 'login',
        pathMatch: 'full',
    },
    {
        path: '**',
        redirectTo: 'login',
    },
];
