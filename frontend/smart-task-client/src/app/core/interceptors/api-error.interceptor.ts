import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';
import { TokenStorageService } from '../services/token-storage.service';
import { SKIP_AUTH_REFRESH } from './http-context.tokens';

export const apiErrorInterceptor: HttpInterceptorFn = (request, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const tokenStorage = inject(TokenStorageService);
  const isApiRequest = request.url.startsWith(environment.apiBaseUrl);
  const isAuthEndpoint = request.url.includes('/v1/auth/');
  const skipAuthRefresh = request.context.get(SKIP_AUTH_REFRESH);
  const accessToken = tokenStorage.getAccessToken();

  // Only requests to the configured API receive credentials. The refresh token stays
  // in an HttpOnly cookie; JavaScript handles only the short-lived access token.
  const apiRequest = isApiRequest
    ? request.clone({
        setHeaders:
          accessToken && !request.url.endsWith('/auth/refresh')
            ? { Authorization: `Bearer ${accessToken}` }
            : {},
        withCredentials: true,
      })
    : request;

  return next(apiRequest).pipe(
    catchError((error: unknown) => {
      if (
        !isApiRequest ||
        !(error instanceof HttpErrorResponse) ||
        error.status !== 401 ||
        isAuthEndpoint ||
        skipAuthRefresh
      ) {
        return throwError(() => error);
      }

      // AuthService coalesces concurrent refresh attempts. The context marker on the
      // retry prevents a second 401 from creating an infinite refresh loop.
      return authService.refresh().pipe(
        switchMap((response) => {
          const refreshedToken = response.data?.accessToken;

          if (!refreshedToken) {
            authService.clearSession();
            void redirectToLogin(router);
            return throwError(() => error);
          }

          const retryRequest = request.clone({
            context: request.context.set(SKIP_AUTH_REFRESH, true),
            setHeaders: { Authorization: `Bearer ${refreshedToken}` },
            withCredentials: true,
          });

          return next(retryRequest);
        }),
        catchError(() => {
          authService.clearSession();
          void redirectToLogin(router);
          return throwError(() => error);
        }),
      );
    }),
  );
};

const redirectToLogin = (router: Router) =>
  router.navigate(['/auth/login'], {
    queryParams: { returnUrl: router.url },
  });
