import {
  HttpClient,
  HttpErrorResponse,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { AuthenticationResponse, UserResponse } from '../models/auth.model';
import { AuthService } from '../services/auth.service';
import { TokenStorageService } from '../services/token-storage.service';
import { apiErrorInterceptor } from './api-error.interceptor';

const testUser: UserResponse = {
  userId: 'user-1',
  email: 'user@example.com',
  firstName: 'Test',
  lastName: 'User',
  roles: ['TeamMember'],
};

describe('apiErrorInterceptor', () => {
  let http: HttpClient;
  let httpTesting: HttpTestingController;
  let tokenStorage: TokenStorageService;
  let authService: AuthService;

  const initialToken = createToken('TeamMember');
  const refreshedToken = createToken('TeamMember');

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([apiErrorInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
    tokenStorage = TestBed.inject(TokenStorageService);
    authService = TestBed.inject(AuthService);
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    tokenStorage.setAccessToken(initialToken);
  });

  afterEach(() => {
    httpTesting.verify();
    authService.clearSession();
  });

  it('refreshes once and retries a protected request with the new token', () => {
    let response: unknown;

    http.get(`${environment.apiBaseUrl}/v1/projects`).subscribe((value) => {
      response = value;
    });

    const originalRequest = httpTesting.expectOne(`${environment.apiBaseUrl}/v1/projects`);
    expect(originalRequest.request.headers.get('Authorization')).toBe(`Bearer ${initialToken}`);
    originalRequest.flush(null, { status: 401, statusText: 'Unauthorized' });

    const refreshRequest = httpTesting.expectOne(`${environment.apiBaseUrl}/v1/auth/refresh`);
    expect(refreshRequest.request.withCredentials).toBe(true);
    refreshRequest.flush(successResponse(createSession(refreshedToken)));

    const retryRequest = httpTesting.expectOne(`${environment.apiBaseUrl}/v1/projects`);
    expect(retryRequest.request.headers.get('Authorization')).toBe(`Bearer ${refreshedToken}`);
    retryRequest.flush({ loaded: true });

    expect(response).toEqual({ loaded: true });
  });

  it('does not retry indefinitely when refresh fails', () => {
    let error: unknown;

    http.get(`${environment.apiBaseUrl}/v1/projects`).subscribe({
      error: (value: unknown) => (error = value),
    });

    const originalRequest = httpTesting.expectOne(`${environment.apiBaseUrl}/v1/projects`);
    originalRequest.flush(null, { status: 401, statusText: 'Unauthorized' });

    const refreshRequest = httpTesting.expectOne(`${environment.apiBaseUrl}/v1/auth/refresh`);
    refreshRequest.flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(error).toBeInstanceOf(HttpErrorResponse);
    httpTesting.expectNone(`${environment.apiBaseUrl}/v1/projects`);
  });
});

function successResponse<T>(data: T): ApiResponse<T> {
  return {
    success: true,
    message: 'Success',
    data,
    errors: null,
    traceId: 'test-trace-id',
  };
}

function createSession(accessToken: string): AuthenticationResponse {
  return {
    accessToken,
    accessTokenExpiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
    user: testUser,
  };
}

function createToken(role: string): string {
  const encode = (value: object) =>
    btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');

  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode({
    exp: Math.floor(Date.now() / 1000) + 3600,
    role,
  })}.signature`;
}
