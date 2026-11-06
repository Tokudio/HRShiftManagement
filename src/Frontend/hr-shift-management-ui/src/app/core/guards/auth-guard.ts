
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

interface StoredUser {
  id: string;
  fullName: string;
  email: string;
  role: string;
}

export const authGuard: CanActivateFn = () => {
  const router = inject(Router);

  const accessToken = localStorage.getItem('access_token');

  if (accessToken) {
    return true;
  }

  return router.createUrlTree(['/login']);
};

export const adminGuard: CanActivateFn = () => {
  const router = inject(Router);

  const accessToken = localStorage.getItem('access_token');
  const userJson = localStorage.getItem('user');

  if (!accessToken || !userJson) {
    return router.createUrlTree(['/login']);
  }

  try {
    const user = JSON.parse(userJson) as StoredUser;

    if (user.role === 'Admin') {
      return true;
    }

    return router.createUrlTree(['/home']);
  } catch {
    return router.createUrlTree(['/login']);
  }
};
