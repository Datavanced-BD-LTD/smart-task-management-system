import { HttpClient, HttpContext } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Observable, catchError, finalize, shareReplay, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SKIP_AUTH_REFRESH } from '../interceptors/http-context.tokens';
import { ApiResponse } from '../models/api-response.model';
import {
  AuthenticationResponse,
  LoginRequest,
  RegisterRequest,
  UserResponse,
} from '../models/auth.model';
import { TokenStorageService } from './token-storage.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenStorage = inject(TokenStorageService);
  private readonly currentUserSignal = signal<UserResponse | null>(
    this.tokenStorage.getCurrentUser(),
  );
  private readonly authenticatedSignal = signal(this.tokenStorage.hasAccessToken());
  private refreshRequest: Observable<ApiResponse<AuthenticationResponse>> | null = null;

  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly currentRoles = computed(
    () => this.currentUserSignal()?.roles ?? this.tokenStorage.getTokenRoles(),
  );
  readonly isAuthenticated = computed(() => this.authenticatedSignal());

  login(request: LoginRequest): Observable<ApiResponse<AuthenticationResponse>> {
    return this.http
      .post<ApiResponse<AuthenticationResponse>>(
        `${environment.apiBaseUrl}/v1/auth/login`,
        request,
        { withCredentials: true },
      )
      .pipe(tap((response) => this.storeSession(response.data)));
  }

  register(request: RegisterRequest): Observable<ApiResponse<UserResponse>> {
    return this.http.post<ApiResponse<UserResponse>>(
      `${environment.apiBaseUrl}/v1/auth/register`,
      request,
    );
  }

  refresh(): Observable<ApiResponse<AuthenticationResponse>> {
    if (this.refreshRequest) {
      return this.refreshRequest;
    }

    const context = new HttpContext().set(SKIP_AUTH_REFRESH, true);
    this.refreshRequest = this.http
      .post<ApiResponse<AuthenticationResponse>>(
        `${environment.apiBaseUrl}/v1/auth/refresh`,
        null,
        { context, withCredentials: true },
      )
      .pipe(
        tap((response) => this.storeSession(response.data)),
        catchError((error: unknown) => {
          this.clearSession();
          return throwError(() => error);
        }),
        finalize(() => {
          this.refreshRequest = null;
        }),
        shareReplay({ bufferSize: 1, refCount: false }),
      );

    return this.refreshRequest;
  }

  getCurrentUser(): Observable<ApiResponse<UserResponse>> {
    return this.http.get<ApiResponse<UserResponse>>(`${environment.apiBaseUrl}/v1/auth/me`);
  }

  logout(): Observable<ApiResponse<null>> {
    return this.http
      .post<ApiResponse<null>>(`${environment.apiBaseUrl}/v1/auth/logout`, null, {
        withCredentials: true,
      })
      .pipe(finalize(() => this.clearSession()));
  }

  clearSession(): void {
    this.tokenStorage.clear();
    this.currentUserSignal.set(null);
    this.authenticatedSignal.set(false);
  }

  private storeSession(session: AuthenticationResponse | null): void {
    if (!session?.accessToken) {
      return;
    }

    this.tokenStorage.setAccessToken(session.accessToken);
    this.tokenStorage.setCurrentUser(session.user);
    this.currentUserSignal.set(session.user);
    this.authenticatedSignal.set(true);
  }
}
