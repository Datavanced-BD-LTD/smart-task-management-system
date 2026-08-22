import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ApiErrorService } from '../../core/services/api-error.service';
import { AuthService } from '../../core/services/auth.service';
import { LoginPageComponent } from './login-page.component';
import { RegisterPageComponent } from './register-page.component';

describe('Authentication pages', () => {
  describe('LoginPageComponent', () => {
    let fixture: ComponentFixture<LoginPageComponent>;
    let component: LoginPageComponent;
    let authService: { login: ReturnType<typeof vi.fn> };

    beforeEach(async () => {
      authService = { login: vi.fn() };

      await TestBed.configureTestingModule({
        imports: [LoginPageComponent],
        providers: [
          provideRouter([]),
          { provide: AuthService, useValue: authService },
          {
            provide: ApiErrorService,
            useValue: { getMessage: vi.fn(), getErrors: vi.fn(() => []) },
          },
        ],
      }).compileComponents();

      fixture = TestBed.createComponent(LoginPageComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();
    });

    it('rejects an invalid login form without calling the API', () => {
      component.submit();

      expect(authService.login).not.toHaveBeenCalled();
      expect(component.getFieldError('email')).toBe('Email is required.');
    });

    it('toggles password visibility', () => {
      expect(component.passwordVisible()).toBe(false);

      component.togglePasswordVisibility();

      expect(component.passwordVisible()).toBe(true);
    });
  });

  describe('RegisterPageComponent', () => {
    let fixture: ComponentFixture<RegisterPageComponent>;
    let component: RegisterPageComponent;
    let authService: { register: ReturnType<typeof vi.fn> };

    beforeEach(async () => {
      authService = { register: vi.fn() };

      await TestBed.configureTestingModule({
        imports: [RegisterPageComponent],
        providers: [
          provideRouter([]),
          { provide: AuthService, useValue: authService },
          {
            provide: ApiErrorService,
            useValue: { getMessage: vi.fn(), getErrors: vi.fn(() => []) },
          },
        ],
      }).compileComponents();

      fixture = TestBed.createComponent(RegisterPageComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();
    });

    it('rejects mismatched passwords before calling the API', () => {
      component.registerForm.setValue({
        firstName: 'Test',
        lastName: 'User',
        email: 'user@example.com',
        password: 'Password1!',
        confirmPassword: 'Different1!',
      });

      component.submit();

      expect(authService.register).not.toHaveBeenCalled();
      expect(component.getFieldError('confirmPassword')).toBe('Passwords do not match.');
    });
  });
});
