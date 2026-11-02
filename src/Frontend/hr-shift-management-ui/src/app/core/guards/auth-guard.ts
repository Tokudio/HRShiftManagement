
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const authGuard: CanActivateFn = () => {
  const router = inject(Router);

  const accessToken = localStorage.getItem('access_token');

  if (accessToken) {
    return true;
  }

  return router.createUrlTree(['/login']);
};
