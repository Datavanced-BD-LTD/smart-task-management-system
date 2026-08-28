import { Component, inject } from '@angular/core';
import {
  MAT_DIALOG_DATA,
  MatDialogModule,
  MatDialogRef,
} from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  CreateManagedUserRequest,
  ManagedUserResponse,
  ManagedUserRole,
  UpdateManagedUserRequest,
  UpdateManagedUserRoleRequest,
} from '../../core/models/admin-user.model';

export interface AdminUserFormDialogData {
  readonly user?: ManagedUserResponse;
  readonly mode?: 'create' | 'edit' | 'role';
}

export type AdminUserFormResult =
  | CreateManagedUserRequest
  | UpdateManagedUserRequest
  | UpdateManagedUserRoleRequest;

@Component({
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  selector: 'app-admin-user-form-dialog',
  template: `
    <h2 mat-dialog-title>{{ mode === 'role' ? 'Change user role' : mode === 'edit' ? 'Edit user' : 'Create user' }}</h2>

    <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
      <mat-dialog-content class="dialog-content">
        @if (mode !== 'role') {
          <div class="name-fields">
            <mat-form-field appearance="outline">
              <mat-label>First name</mat-label>
              <input matInput formControlName="firstName" autocomplete="given-name" />
              @if (getError('firstName')) { <mat-error>{{ getError('firstName') }}</mat-error> }
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Last name</mat-label>
              <input matInput formControlName="lastName" autocomplete="family-name" />
              @if (getError('lastName')) { <mat-error>{{ getError('lastName') }}</mat-error> }
            </mat-form-field>
          </div>

          <mat-form-field appearance="outline">
            <mat-label>Email</mat-label>
            <input matInput type="email" formControlName="email" autocomplete="email" />
            @if (getError('email')) { <mat-error>{{ getError('email') }}</mat-error> }
          </mat-form-field>

          @if (mode === 'create') {
            <mat-form-field appearance="outline">
              <mat-label>Temporary password</mat-label>
              <input
                matInput
                [type]="passwordVisible ? 'text' : 'password'"
                formControlName="password"
                autocomplete="new-password"
              />
              <button mat-button matSuffix type="button" (click)="passwordVisible = !passwordVisible">
                {{ passwordVisible ? 'Hide' : 'Show' }}
              </button>
              @if (getError('password')) { <mat-error>{{ getError('password') }}</mat-error> }
            </mat-form-field>
          }
        }

        @if (mode !== 'edit') {
          <mat-form-field appearance="outline">
            <mat-label>Role</mat-label>
            <mat-select formControlName="role" aria-label="User role">
              <mat-option value="ProjectManager">Project Manager</mat-option>
              <mat-option value="TeamMember">Team Member</mat-option>
            </mat-select>
            @if (getError('role')) { <mat-error>{{ getError('role') }}</mat-error> }
          </mat-form-field>
        }

        @if (mode === 'create') {
          <p class="help-text">The user will receive the selected role immediately.</p>
        }
      </mat-dialog-content>

      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>Cancel</button>
        <button mat-flat-button type="submit">{{ mode === 'role' ? 'Save role' : mode === 'edit' ? 'Save changes' : 'Create user' }}</button>
      </mat-dialog-actions>
    </form>
  `,
  styles: `
    .dialog-content { display: grid; gap: 0.5rem; min-width: min(34rem, 78vw); padding-top: 0.5rem; }
    .name-fields { display: grid; gap: 0.75rem; grid-template-columns: 1fr 1fr; }
    mat-form-field { width: 100%; }
    .help-text { color: #5f6368; font-size: 0.85rem; margin: 0; }
    @media (max-width: 600px) { .name-fields { grid-template-columns: 1fr; } }
  `,
})
export class AdminUserFormDialogComponent {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly dialogRef = inject(MatDialogRef<AdminUserFormDialogComponent>);
  private readonly data = inject<AdminUserFormDialogData>(MAT_DIALOG_DATA);

  readonly user = this.data.user;
  readonly mode = this.data.mode ?? (this.user ? 'role' : 'create');
  passwordVisible = false;
  readonly form = this.formBuilder.group({
    firstName: [this.user?.firstName ?? '', [Validators.required, Validators.maxLength(100)]],
    lastName: [this.user?.lastName ?? '', [Validators.required, Validators.maxLength(100)]],
    email: [this.user?.email ?? '', [Validators.required, Validators.email, Validators.maxLength(256)]],
    password: [
      '',
      this.mode === 'create' ? [Validators.required, Validators.minLength(8), Validators.maxLength(128)] : [],
    ],
    role: [this.initialRole(), [Validators.required]],
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const role = value.role as ManagedUserRole;
    const result: AdminUserFormResult = this.mode === 'role'
      ? { role }
      : this.mode === 'edit'
        ? {
            firstName: value.firstName.trim(),
            lastName: value.lastName.trim(),
            email: value.email.trim(),
          }
        : {
          firstName: value.firstName.trim(),
          lastName: value.lastName.trim(),
          email: value.email.trim(),
          password: value.password,
          role,
        };

    this.dialogRef.close(result);
  }

  getError(field: 'firstName' | 'lastName' | 'email' | 'password' | 'role'): string {
    const control = this.form.controls[field];
    if (!control.touched && !control.dirty) return '';
    if (control.hasError('required')) return `${this.fieldLabel(field)} is required.`;
    if (control.hasError('email')) return 'Enter a valid email address.';
    if (control.hasError('minlength')) return 'Password must contain at least 8 characters.';
    if (control.hasError('maxlength')) return `${this.fieldLabel(field)} is too long.`;
    return '';
  }

  private initialRole(): ManagedUserRole {
    const role = this.user?.roles.find((candidate) => candidate === 'ProjectManager');
    return role === 'ProjectManager' ? 'ProjectManager' : 'TeamMember';
  }

  private fieldLabel(field: string): string {
    return field === 'firstName'
      ? 'First name'
      : field === 'lastName'
        ? 'Last name'
        : field === 'email'
          ? 'Email'
          : field === 'password'
            ? 'Password'
            : 'Role';
  }
}
