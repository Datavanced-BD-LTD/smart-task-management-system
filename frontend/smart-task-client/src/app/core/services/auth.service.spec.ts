import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { SKIP_AUTH_REFRESH } from '../interceptors/http-context.tokens';
import { ApiResponse } from '../models/api-response.model';
import { AuthenticationResponse, UserResponse } from '../models/auth.model';
import { AuthService } from './auth.service';
import { TokenStorageService } from './token-storage.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpTesting: HttpTestingController;
  let tokenStorage: TokenStorageService;

  const user: UserResponse = {
    userId: 'user-1',
    email: 'user@example.com',
    firstName: 'Test',
    lastName: 'User',
    roles: ['TeamMember'],
  };
  const session: AuthenticationResponse = {
    accessToken: createToken('TeamMember'),
    accessTokenExpiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
    user,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(AuthService);
    httpTesting = TestBed.inject(HttpTestingController);
    tokenStorage = TestBed.inject(TokenStorageService);
    tokenStorage.clear();
    service.clearSession();
  });

  afterEach(() => {
    httpTesting.verify();
    service.clearSession();
  });

  it('stores the access token, user, and roles after login', () => {
    let response: ApiResponse<AuthenticationResponse> | undefined;

    service.login({ email: user.email, password: 'Password1!' }).subscribe((value) => {
      response = value;
    });

    const request = httpTesting.expectOne(`${environment.apiBaseUrl}/v1/auth/login`);
    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBe(true);
    request.flush(successResponse(session, 'Login successful.'));

    expect(response?.data?.user).toEqual(user);
    expect(tokenStorage.getAccessToken()).toBe(session.accessToken);
    expect(service.currentUser()).toEqual(user);
    expect(service.currentRoles()).toEqual(['TeamMember']);
    expect(service.isAuthenticated()).toBe(true);
  });

  it('uses the HttpOnly cookie for refresh and shares concurrent refresh requests', () => {
    let firstResponse: ApiResponse<AuthenticationResponse> | undefined;
    let secondResponse: ApiResponse<AuthenticationResponse> | undefined;

    service.refresh().subscribe((value) => (firstResponse = value));
    service.refresh().subscribe((value) => (secondResponse = value));

    const request = httpTesting.expectOne(`${environment.apiBaseUrl}/v1/auth/refresh`);
    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.context.get(SKIP_AUTH_REFRESH)).toBe(true);
    request.flush(successResponse(session, 'Access token refreshed successfully.'));

    expect(firstResponse?.data?.accessToken).toBe(session.accessToken);
    expect(secondResponse?.data?.accessToken).toBe(session.accessToken);
  });

  it('clears the client session on logout', () => {
    tokenStorage.setAccessToken(session.accessToken);
    tokenStorage.setCurrentUser(user);

    service.logout().subscribe();

    const request = httpTesting.expectOne(`${environment.apiBaseUrl}/v1/auth/logout`);
    expect(request.request.withCredentials).toBe(true);
    request.flush(successResponse(null, 'Logout successful.'));

    expect(tokenStorage.getAccessToken()).toBeNull();
    expect(service.currentUser()).toBeNull();
    expect(service.isAuthenticated()).toBe(false);
  });
});

function successResponse<T>(data: T, message: string): ApiResponse<T> {
  return {
    success: true,
    message,
    data,
    errors: null,
    traceId: 'test-trace-id',
  };
}

function createToken(role: string, expiresAt = Math.floor(Date.now() / 1000) + 3600): string {
  const header = encodeBase64Url({ alg: 'none', typ: 'JWT' });
  const payload = encodeBase64Url({ exp: expiresAt, role });

  return `${header}.${payload}.signature`;
}

function encodeBase64Url(value: object): string {
  return btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}
