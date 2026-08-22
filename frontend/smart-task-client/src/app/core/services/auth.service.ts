import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Observable, finalize, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
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
  private readonly authenticatedSignal = signal(
    this.tokenStorage.hasAccessToken(),
  );

  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.authenticatedSignal());

  login(
    request: LoginRequest,
  ): Observable<ApiResponse<AuthenticationResponse>> {
    return this.http
      .post<ApiResponse<AuthenticationResponse>>(
        `${environment.apiBaseUrl}/v1/auth/login`,
        request,
        { withCredentials: true },
      )
      .pipe(tap((response) => this.storeSession(response.data)));
  }

  register(
    request: RegisterRequest,
  ): Observable<ApiResponse<UserResponse>> {
    return this.http.post<ApiResponse<UserResponse>>(
      `${environment.apiBaseUrl}/v1/auth/register`,
      request,
    );
  }

  refresh(): Observable<ApiResponse<AuthenticationResponse>> {
    return this.http
      .post<ApiResponse<AuthenticationResponse>>(
        `${environment.apiBaseUrl}/v1/auth/refresh`,
        null,
        { withCredentials: true },
      )
      .pipe(tap((response) => this.storeSession(response.data)));
  }

  logout(): Observable<ApiResponse<null>> {
    return this.http
      .post<ApiResponse<null>>(
        `${environment.apiBaseUrl}/v1/auth/logout`,
        null,
        { withCredentials: true },
      )
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
