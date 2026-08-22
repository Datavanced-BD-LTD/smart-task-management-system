import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { TokenStorageService } from '../services/token-storage.service';

export const authGuard = () => {
  const tokenStorage = inject(TokenStorageService);
  const router = inject(Router);

  return tokenStorage.hasAccessToken()
    ? true
    : router.createUrlTree(['/auth/login']);
};
