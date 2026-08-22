import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ProjectMemberResponse } from '../../core/models/project.model';
import {
  CreateTaskRequest,
  TASK_PRIORITY_DEFINITIONS,
  TASK_STATUS_DEFINITIONS,
  TaskPriority,
  TaskResponse,
  TaskStatus,
} from '../../core/models/task.model';

export interface TaskFormDialogData {
  readonly task?: TaskResponse;
  readonly members: readonly ProjectMemberResponse[];
}

@Component({
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  selector: 'app-task-form-dialog',
  template: `
    <h2 mat-dialog-title>{{ task ? 'Edit task' : 'Create task' }}</h2>

    <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
      <mat-dialog-content class="dialog-content">
        <mat-form-field appearance="outline">
          <mat-label>Title</mat-label>
          <input matInput formControlName="title" maxlength="200" autocomplete="off" />
          @if (getError('title')) {
            <mat-error>{{ getError('title') }}</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Description</mat-label>
          <textarea matInput formControlName="description" maxlength="2000" rows="4"></textarea>
          @if (getError('description')) {
            <mat-error>{{ getError('description') }}</mat-error>
          }
        </mat-form-field>

        <div class="form-grid">
          <mat-form-field appearance="outline">
            <mat-label>Assignee</mat-label>
            <mat-select formControlName="assignedToUserId">
              <mat-option value="">Unassigned</mat-option>
              @for (member of members; track member.userId) {
                <mat-option [value]="member.userId">
                  {{ member.firstName }} {{ member.lastName }}
                </mat-option>
              }
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Due date</mat-label>
            <input matInput type="date" formControlName="dueDate" />
            @if (getError('dueDate')) {
              <mat-error>{{ getError('dueDate') }}</mat-error>
            }
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Status</mat-label>
            <mat-select formControlName="status">
              @for (status of statuses; track status.key) {
                <mat-option [value]="status.key">{{ status.label }}</mat-option>
              }
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Priority</mat-label>
            <mat-select formControlName="priority">
              @for (priority of priorities; track priority.key) {
                <mat-option [value]="priority.key">{{ priority.label }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
        </div>
      </mat-dialog-content>

      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>Cancel</button>
        <button mat-flat-button type="submit">
          {{ task ? 'Save changes' : 'Create task' }}
        </button>
      </mat-dialog-actions>
    </form>
  `,
  styles: `
    .dialog-content {
      display: grid;
      gap: 0.5rem;
      min-width: min(42rem, 78vw);
      padding-top: 0.5rem;
    }

    .form-grid {
      display: grid;
      gap: 0.5rem;
      grid-template-columns: repeat(2, minmax(0, 1fr));
    }

    mat-form-field {
      width: 100%;
    }

    @media (max-width: 620px) {
      .dialog-content {
        min-width: auto;
      }

      .form-grid {
        grid-template-columns: 1fr;
      }
    }
  `,
})
export class TaskFormDialogComponent {
  private readonly data = inject<TaskFormDialogData>(MAT_DIALOG_DATA);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly dialogRef = inject(MatDialogRef<TaskFormDialogComponent>);

  readonly task = this.data.task;
  readonly members = this.data.members;
  readonly statuses = TASK_STATUS_DEFINITIONS;
  readonly priorities = TASK_PRIORITY_DEFINITIONS;
  readonly form = this.formBuilder.group({
    title: [this.task?.title ?? '', [Validators.required, Validators.maxLength(200)]],
    description: [this.task?.description ?? '', [Validators.maxLength(2000)]],
    assignedToUserId: [this.task?.assignedToUserId ?? ''],
    status: this.formBuilder.control<TaskStatus>(this.task?.status ?? 0),
    priority: this.formBuilder.control<TaskPriority>(this.task?.priority ?? 1),
    dueDate: [this.toDateInput(this.task?.dueDate)],
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const request: CreateTaskRequest = {
      title: value.title.trim(),
      description: value.description.trim() || null,
      assignedToUserId: value.assignedToUserId || null,
      status: value.status,
      priority: value.priority,
      dueDate: value.dueDate || null,
    };

    this.dialogRef.close(request);
  }

  getError(field: 'title' | 'description' | 'dueDate'): string {
    const control = this.form.controls[field];

    if (!control.touched && !control.dirty) {
      return '';
    }

    if (control.hasError('required')) {
      return 'Task title is required.';
    }

    if (control.hasError('maxlength')) {
      return field === 'title'
        ? 'Task title cannot exceed 200 characters.'
        : 'Description cannot exceed 2,000 characters.';
    }

    return '';
  }

  private toDateInput(value: string | null | undefined): string {
    return value ? value.slice(0, 10) : '';
  }
}
