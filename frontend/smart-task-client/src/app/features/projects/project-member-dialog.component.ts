import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { AddProjectMemberRequest } from '../../core/models/project.model';

const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

@Component({
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    ReactiveFormsModule,
  ],
  selector: 'app-project-member-dialog',
  template: `
    <h2 mat-dialog-title>Add project member</h2>

    <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
      <mat-dialog-content class="dialog-content">
        <p>Only active Team Members can be added to a project.</p>
        <mat-form-field appearance="outline">
          <mat-label>Team Member User ID</mat-label>
          <input matInput formControlName="userId" autocomplete="off" />
          @if (errorMessage()) {
            <mat-error>{{ errorMessage() }}</mat-error>
          }
        </mat-form-field>
      </mat-dialog-content>

      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>Cancel</button>
        <button mat-flat-button type="submit">Add member</button>
      </mat-dialog-actions>
    </form>
  `,
  styles: `
    .dialog-content {
      min-width: min(32rem, 75vw);
      padding-top: 0.5rem;
    }

    p {
      color: #5f6368;
      margin-top: 0;
    }

    mat-form-field {
      width: 100%;
    }
  `,
})
export class ProjectMemberDialogComponent {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly dialogRef = inject(MatDialogRef<ProjectMemberDialogComponent>);

  readonly form = this.formBuilder.group({
    userId: ['', [Validators.required, Validators.pattern(UUID_PATTERN)]],
  });

  errorMessage(): string {
    const control = this.form.controls.userId;

    if (!control.touched && !control.dirty) {
      return '';
    }

    if (control.hasError('required')) {
      return 'User ID is required.';
    }

    return control.hasError('pattern') ? 'Enter a valid user ID.' : '';
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request: AddProjectMemberRequest = {
      userId: this.form.controls.userId.value.trim(),
    };
    this.dialogRef.close(request);
  }
}
