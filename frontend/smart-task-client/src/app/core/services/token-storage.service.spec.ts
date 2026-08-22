import { TestBed } from '@angular/core/testing';
import { UserResponse } from '../models/auth.model';
import { TokenStorageService } from './token-storage.service';

describe('TokenStorageService', () => {
  let service: TokenStorageService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(TokenStorageService);
    service.clear();
  });

  afterEach(() => service.clear());

  it('does not treat expired access tokens as authenticated', () => {
    service.setAccessToken(createToken('Admin', Math.floor(Date.now() / 1000) - 1));

    expect(service.getAccessToken()).toBeNull();
    expect(service.hasAccessToken()).toBe(false);
  });

  it('reads role claims without exposing the token value', () => {
    const user: UserResponse = {
      userId: 'user-1',
      email: 'admin@example.com',
      firstName: 'Admin',
      lastName: 'User',
      roles: ['Admin'],
    };
    service.setAccessToken(createToken('Admin'));
    service.setCurrentUser(user);

    expect(service.getTokenRoles()).toEqual(['Admin']);
    expect(service.getCurrentUser()).toEqual(user);
  });
});

function createToken(role: string, expiresAt = Math.floor(Date.now() / 1000) + 3600): string {
  const encode = (value: object) =>
    btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');

  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode({
    exp: expiresAt,
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': role,
  })}.signature`;
}
