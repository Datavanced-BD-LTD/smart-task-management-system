import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const isAdmin = authService
    .currentRoles()
    .some((role) => role.toLowerCase() === 'admin');

  return isAdmin ? true : router.createUrlTree(['/dashboard']);
};
