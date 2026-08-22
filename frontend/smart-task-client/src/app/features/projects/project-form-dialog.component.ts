import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { AuthService } from '../../core/services/auth.service';
import { CreateProjectRequest, ProjectResponse } from '../../core/models/project.model';

export interface ProjectFormDialogData {
  readonly project?: ProjectResponse;
}

const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

@Component({
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    ReactiveFormsModule,
  ],
  selector: 'app-project-form-dialog',
  template: `
    <h2 mat-dialog-title>{{ project ? 'Edit project' : 'Create project' }}</h2>

    <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
      <mat-dialog-content class="dialog-content">
        <mat-form-field appearance="outline">
          <mat-label>Project name</mat-label>
          <input matInput formControlName="name" maxlength="200" autocomplete="off" />
          @if (getError('name')) {
            <mat-error>{{ getError('name') }}</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Description</mat-label>
          <textarea matInput formControlName="description" maxlength="2000" rows="4"></textarea>
          @if (getError('description')) {
            <mat-error>{{ getError('description') }}</mat-error>
          }
        </mat-form-field>

        @if (isAdmin) {
          <div class="manager-summary" aria-describedby="project-manager-help">
            <span class="summary-label">Project Manager</span>
            <strong>{{ projectManagerDisplayName() }}</strong>
            @if (projectManagerEmail()) {
              <span class="summary-detail">{{ projectManagerEmail() }}</span>
            }
            <span id="project-manager-help" class="summary-help">
              The current manager is kept when editing this project.
            </span>
          </div>
          <input type="hidden" formControlName="projectManagerId" />
        }
      </mat-dialog-content>

      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>Cancel</button>
        <button mat-flat-button type="submit">
          {{ project ? 'Save changes' : 'Create project' }}
        </button>
      </mat-dialog-actions>
    </form>
  `,
  styles: `
    .dialog-content {
      display: grid;
      gap: 0.5rem;
      min-width: min(32rem, 75vw);
      padding-top: 0.5rem;
    }

    mat-form-field {
      width: 100%;
    }

    .manager-summary {
      background: #f1f4fb;
      border-radius: 0.5rem;
      display: grid;
      gap: 0.25rem;
      padding: 0.75rem;
    }

    .summary-label,
    .summary-help,
    .summary-detail {
      color: #5f6368;
      font-size: 0.85rem;
    }
  `,
})
export class ProjectFormDialogComponent {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly dialogRef = inject(MatDialogRef<ProjectFormDialogComponent>);
  private readonly authService = inject(AuthService);
  private readonly data = inject<ProjectFormDialogData>(MAT_DIALOG_DATA);

  readonly project = this.data.project;
  readonly isAdmin = this.authService.currentRoles().some((role) => role === 'Admin');
  readonly form = this.formBuilder.group({
    name: [this.project?.name ?? '', [Validators.required, Validators.maxLength(200)]],
    description: [this.project?.description ?? '', [Validators.maxLength(2000)]],
    projectManagerId: [this.project?.projectManagerId ?? '', [Validators.pattern(UUID_PATTERN)]],
  });

  projectManagerDisplayName(): string {
    return (
      this.project?.projectManagerName?.trim() ||
      this.project?.projectManagerEmail?.trim() ||
      (this.project
        ? 'Unknown user'
        : `${this.authService.currentUser()?.firstName ?? ''} ${this.authService.currentUser()?.lastName ?? ''}`.trim() ||
          this.authService.currentUser()?.email ||
          'Signed-in administrator')
    );
  }

  projectManagerEmail(): string | null {
    return this.project?.projectManagerEmail ??
      (!this.project ? this.authService.currentUser()?.email ?? null : null);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const request: CreateProjectRequest = {
      name: value.name.trim(),
      description: value.description.trim() || null,
      projectManagerId: value.projectManagerId.trim() || null,
    };

    this.dialogRef.close(request);
  }

  getError(field: 'name' | 'description' | 'projectManagerId'): string {
    const control = this.form.controls[field];

    if (!control.touched && !control.dirty) {
      return '';
    }

    if (control.hasError('required')) {
      return 'Project name is required.';
    }

    if (control.hasError('maxlength')) {
      return field === 'name'
        ? 'Project name cannot exceed 200 characters.'
        : 'Description cannot exceed 2,000 characters.';
    }

    if (control.hasError('pattern')) {
      return 'The project manager selection is invalid.';
    }

    return '';
  }
}
