import { Injectable } from '@angular/core';
import { UserResponse } from '../models/auth.model';

@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  // Keeping the access token in memory limits persistence after a tab is closed. The
  // backend-issued HttpOnly refresh cookie restores a session when appropriate.
  private accessToken: string | null = null;
  private currentUser: UserResponse | null = null;

  getAccessToken(): string | null {
    const accessToken = this.accessToken;

    return accessToken && this.isTokenValid(accessToken) ? accessToken : null;
  }

  setAccessToken(accessToken: string): void {
    this.accessToken = accessToken;
  }

  getCurrentUser(): UserResponse | null {
    return this.currentUser;
  }

  setCurrentUser(user: UserResponse): void {
    this.currentUser = user;
  }

  clear(): void {
    this.accessToken = null;
    this.currentUser = null;
  }

  hasAccessToken(): boolean {
    return Boolean(this.getAccessToken());
  }

  getTokenRoles(): readonly string[] {
    const accessToken = this.accessToken;
    const payload = accessToken ? this.decodePayload(accessToken) : null;
    const roleClaim =
      payload?.['role'] ??
      payload?.['roles'] ??
      payload?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

    if (typeof roleClaim === 'string') {
      return [roleClaim];
    }

    return Array.isArray(roleClaim)
      ? roleClaim.filter((role): role is string => typeof role === 'string')
      : [];
  }

  private isTokenValid(accessToken: string): boolean {
    const payload = this.decodePayload(accessToken);
    const expiresAt = payload?.['exp'];

    // The small safety window avoids sending a token that will expire during transit.
    return typeof expiresAt === 'number' && expiresAt > Math.floor(Date.now() / 1000) + 30;
  }

  private decodePayload(accessToken: string): Record<string, unknown> | null {
    const encodedPayload = accessToken.split('.')[1];

    if (!encodedPayload) {
      return null;
    }

    try {
      const normalizedPayload = encodedPayload.replace(/-/g, '+').replace(/_/g, '/');
      const paddedPayload = normalizedPayload.padEnd(
        normalizedPayload.length + ((4 - (normalizedPayload.length % 4)) % 4),
        '=',
      );

      return JSON.parse(atob(paddedPayload)) as Record<string, unknown>;
    } catch {
      return null;
    }
  }
}
