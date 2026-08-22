import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  provideRouter,
  Router,
  RouterStateSnapshot,
} from '@angular/router';
import { firstValueFrom, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  it('redirects unauthenticated users to login with their return URL', async () => {
    const authService = {
      isAuthenticated: vi.fn(() => false),
      refresh: vi.fn(() => throwError(() => new Error('No session'))),
    };

    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AuthService, useValue: authService }],
    });

    const router = TestBed.inject(Router);
    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, { url: '/dashboard' } as RouterStateSnapshot),
    );
    const guardResult = await firstValueFrom(result as ReturnType<typeof throwError>);

    expect(router.serializeUrl(guardResult as ReturnType<typeof router.createUrlTree>)).toBe(
      '/auth/login?returnUrl=%2Fdashboard',
    );
  });
});
