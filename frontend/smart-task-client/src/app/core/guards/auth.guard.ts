import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  return authService.refresh().pipe(
    map((response) =>
      response.data
        ? true
        : router.createUrlTree(['/auth/login'], {
            queryParams: { returnUrl: state.url },
          }),
    ),
    catchError(() =>
      of(
        router.createUrlTree(['/auth/login'], {
          queryParams: { returnUrl: state.url },
        }),
      ),
    ),
  );
};
