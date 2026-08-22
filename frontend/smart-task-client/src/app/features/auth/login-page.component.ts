import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, NonNullableFormBuilder, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ApiError } from '../../core/models/api-response.model';
import { ApiErrorService } from '../../core/services/api-error.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  imports: [
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  selector: 'app-login-page',
  templateUrl: './login-page.component.html',
  styleUrl: './login-page.component.scss',
})
export class LoginPageComponent {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly authService = inject(AuthService);
  private readonly apiErrorService = inject(ApiErrorService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly loginForm = this.formBuilder.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    password: ['', [Validators.required]],
  });
  readonly loading = signal(false);
  readonly submitted = signal(false);
  readonly passwordVisible = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly apiErrors = signal<readonly ApiError[]>([]);
  readonly registrationMessage = signal<string | null>(
    this.route.snapshot.queryParamMap.has('registered')
      ? 'Registration successful. You can now sign in.'
      : null,
  );

  submit(): void {
    this.submitted.set(true);
    this.errorMessage.set(null);
    this.apiErrors.set([]);

    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.authService
      .login(this.loginForm.getRawValue())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          if (!response.data) {
            this.errorMessage.set('The login response was invalid. Please try again.');
            return;
          }

          const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
          const destination = returnUrl?.startsWith('/') ? returnUrl : '/dashboard';

          void this.router.navigateByUrl(destination);
        },
        error: (error: unknown) => this.displayApiError(error),
      });
  }

  getFieldError(field: 'email' | 'password'): string {
    const control = this.loginForm.controls[field];

    if (!this.submitted() && !control.touched) {
      return '';
    }

    if (control.hasError('required')) {
      return field === 'email' ? 'Email is required.' : 'Password is required.';
    }

    if (control.hasError('email')) {
      return 'Enter a valid email address.';
    }

    if (control.hasError('maxlength')) {
      return 'Email cannot exceed 256 characters.';
    }

    return '';
  }

  togglePasswordVisibility(): void {
    this.passwordVisible.update((visible) => !visible);
  }

  private displayApiError(error: unknown): void {
    this.errorMessage.set(this.apiErrorService.getMessage(error));
    this.apiErrors.set(this.apiErrorService.getErrors(error));
  }
}
