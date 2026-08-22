import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatNativeDateModule, provideNativeDateAdapter } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
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
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatNativeDateModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  providers: [provideNativeDateAdapter()],
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
            <input
              matInput
              [matDatepicker]="dueDatePicker"
              formControlName="dueDate"
              (click)="dueDatePicker.open()"
              aria-label="Due date"
            />
            <mat-datepicker-toggle matIconSuffix [for]="dueDatePicker"></mat-datepicker-toggle>
            <mat-datepicker #dueDatePicker></mat-datepicker>
            <mat-hint>Optional</mat-hint>
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
    :host {
      display: block;
      min-width: 0;
      max-width: 100%;
    }

    form {
      min-width: 0;
    }

    .dialog-content {
      box-sizing: border-box;
      display: grid;
      gap: 1rem;
      width: 100%;
      max-width: 100%;
      min-width: 0;
      max-height: 70vh;
      overflow-x: hidden;
      padding-top: 0.5rem;
    }

    .form-grid {
      display: grid;
      width: 100%;
      min-width: 0;
      gap: 1rem;
      grid-template-columns: repeat(2, minmax(0, 1fr));
    }

    mat-form-field {
      width: 100%;
      min-width: 0;
    }

    input,
    textarea {
      box-sizing: border-box;
      max-width: 100%;
    }

    mat-dialog-actions {
      box-sizing: border-box;
      gap: 0.5rem;
      flex-wrap: wrap;
      padding: 0.75rem 0;
    }

    @media (max-width: 620px) {
      .dialog-content {
        max-height: 62vh;
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
    dueDate: this.formBuilder.control<Date | null>(this.toDateValue(this.task?.dueDate)),
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
      dueDate: this.toApiDate(value.dueDate),
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

  private toDateValue(value: string | null | undefined): Date | null {
    const datePart = value?.slice(0, 10);

    if (!datePart) {
      return null;
    }

    const [year = 0, month = 0, day = 0] = datePart.split('-').map(Number);
    const date = new Date(year, month - 1, day);

    return date.getFullYear() === year &&
      date.getMonth() === month - 1 &&
      date.getDate() === day
      ? date
      : null;
  }

  private toApiDate(value: Date | null): string | null {
    if (!value || Number.isNaN(value.getTime())) {
      return null;
    }

    const year = value.getFullYear();
    const month = String(value.getMonth() + 1).padStart(2, '0');
    const day = String(value.getDate()).padStart(2, '0');

    return `${year}-${month}-${day}`;
  }
}
