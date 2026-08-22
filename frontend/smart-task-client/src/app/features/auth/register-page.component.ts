import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router, RouterLink } from '@angular/router';
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
  selector: 'app-register-page',
  templateUrl: './register-page.component.html',
  styleUrl: './register-page.component.scss',
})
export class RegisterPageComponent {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly authService = inject(AuthService);
  private readonly apiErrorService = inject(ApiErrorService);
  private readonly router = inject(Router);

  readonly registerForm = this.formBuilder.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    password: [
      '',
      [
        Validators.required,
        Validators.minLength(8),
        Validators.maxLength(128),
        Validators.pattern(/[A-Z]/),
        Validators.pattern(/[a-z]/),
        Validators.pattern(/[0-9]/),
        Validators.pattern(/[^a-zA-Z0-9]/),
      ],
    ],
    confirmPassword: ['', [Validators.required]],
  });
  readonly loading = signal(false);
  readonly submitted = signal(false);
  readonly passwordVisible = signal(false);
  readonly confirmPasswordVisible = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly apiErrors = signal<readonly ApiError[]>([]);

  submit(): void {
    this.submitted.set(true);
    this.errorMessage.set(null);
    this.apiErrors.set([]);

    if (
      this.registerForm.controls.password.value !== this.registerForm.controls.confirmPassword.value
    ) {
      this.registerForm.controls.confirmPassword.setErrors({ mismatch: true });
      this.registerForm.controls.confirmPassword.markAsTouched();
      return;
    }

    if (this.registerForm.controls.confirmPassword.hasError('mismatch')) {
      this.registerForm.controls.confirmPassword.setErrors(null);
    }

    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    const { confirmPassword: _confirmPassword, ...request } = this.registerForm.getRawValue();

    this.authService
      .register(request)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () =>
          void this.router.navigate(['/auth/login'], {
            queryParams: { registered: 'true' },
          }),
        error: (error: unknown) => this.displayApiError(error),
      });
  }

  getFieldError(
    field: 'firstName' | 'lastName' | 'email' | 'password' | 'confirmPassword',
  ): string {
    const control = this.registerForm.controls[field];

    if (!this.submitted() && !control.touched) {
      return '';
    }

    if (control.hasError('required')) {
      return `${this.getFieldLabel(field)} is required.`;
    }

    if (control.hasError('email')) {
      return 'Enter a valid email address.';
    }

    if (control.hasError('minlength')) {
      return 'Password must be at least 8 characters long.';
    }

    if (control.hasError('maxlength')) {
      const maximumLength = field === 'email' ? 256 : field === 'password' ? 128 : 100;
      return `${this.getFieldLabel(field)} cannot exceed ${maximumLength} characters.`;
    }

    if (control.hasError('pattern')) {
      return 'Password must contain uppercase, lowercase, a digit, and a special character.';
    }

    if (control.hasError('mismatch')) {
      return 'Passwords do not match.';
    }

    return '';
  }

  togglePasswordVisibility(): void {
    this.passwordVisible.update((visible) => !visible);
  }

  toggleConfirmPasswordVisibility(): void {
    this.confirmPasswordVisible.update((visible) => !visible);
  }

  private getFieldLabel(field: string): string {
    return field === 'firstName'
      ? 'First name'
      : field === 'lastName'
        ? 'Last name'
        : field === 'confirmPassword'
          ? 'Password confirmation'
          : field.charAt(0).toUpperCase() + field.slice(1);
  }

  private displayApiError(error: unknown): void {
    this.errorMessage.set(this.apiErrorService.getMessage(error));
    this.apiErrors.set(this.apiErrorService.getErrors(error));
  }
}
