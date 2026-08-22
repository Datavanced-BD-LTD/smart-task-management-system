import {
  HttpErrorResponse,
  HttpInterceptorFn,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TokenStorageService } from '../services/token-storage.service';

export const apiErrorInterceptor: HttpInterceptorFn = (request, next) => {
  const tokenStorage = inject(TokenStorageService);
  const isApiRequest = request.url.startsWith(environment.apiBaseUrl);
  const accessToken = tokenStorage.getAccessToken();
  const headers: Record<string, string> = {};

  if (isApiRequest && accessToken) {
    headers['Authorization'] = `Bearer ${accessToken}`;
  }

  const apiRequest = isApiRequest
    ? request.clone({
        setHeaders: headers,
        withCredentials: true,
      })
    : request;

  return next(apiRequest).pipe(
    catchError((error: unknown) => {
      if (
        isApiRequest &&
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        !request.url.endsWith('/auth/login')
      ) {
        tokenStorage.clear();
      }

      return throwError(() => error);
    }),
  );
};
