import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { inject, Injectable, PLATFORM_ID } from '@angular/core';
import { UserResponse } from '../models/auth.model';

@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  private readonly accessTokenKey = 'smart-task-access-token';
  private readonly currentUserKey = 'smart-task-current-user';
  private readonly document = inject(DOCUMENT);
  private readonly platformId = inject(PLATFORM_ID);

  getAccessToken(): string | null {
    return this.storage?.getItem(this.accessTokenKey) ?? null;
  }

  setAccessToken(accessToken: string): void {
    this.storage?.setItem(this.accessTokenKey, accessToken);
  }

  getCurrentUser(): UserResponse | null {
    const serializedUser = this.storage?.getItem(this.currentUserKey);

    if (!serializedUser) {
      return null;
    }

    try {
      return JSON.parse(serializedUser) as UserResponse;
    } catch {
      this.storage?.removeItem(this.currentUserKey);
      return null;
    }
  }

  setCurrentUser(user: UserResponse): void {
    this.storage?.setItem(this.currentUserKey, JSON.stringify(user));
  }

  clear(): void {
    this.storage?.removeItem(this.accessTokenKey);
    this.storage?.removeItem(this.currentUserKey);
  }

  hasAccessToken(): boolean {
    return Boolean(this.getAccessToken());
  }

  private get storage(): Storage | null {
    return isPlatformBrowser(this.platformId)
      ? this.document.defaultView?.localStorage ?? null
      : null;
  }
}
