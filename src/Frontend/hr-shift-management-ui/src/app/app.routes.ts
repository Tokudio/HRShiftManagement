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
