import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { ProjectMemberResponse } from '../../core/models/project.model';
import { AssignTaskRequest, TaskResponse } from '../../core/models/task.model';

export interface TaskAssignmentDialogData {
  readonly task: TaskResponse;
  readonly members: readonly ProjectMemberResponse[];
}

@Component({
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  selector: 'app-task-assignment-dialog',
  template: `
    <h2 mat-dialog-title>Assign task</h2>

    <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
      <mat-dialog-content class="dialog-content">
        <p>Choose an active member of this project or leave the task unassigned.</p>
        <mat-form-field appearance="outline">
          <mat-label>Assignee</mat-label>
          <mat-select formControlName="assignedUserId">
            <mat-option value="">Unassigned</mat-option>
            @for (member of data.members; track member.userId) {
              <mat-option [value]="member.userId">
                {{ member.firstName }} {{ member.lastName }}
              </mat-option>
            }
          </mat-select>
        </mat-form-field>
      </mat-dialog-content>

      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>Cancel</button>
        <button mat-flat-button type="submit">Save assignment</button>
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
export class TaskAssignmentDialogComponent {
  readonly data = inject<TaskAssignmentDialogData>(MAT_DIALOG_DATA);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly dialogRef = inject(MatDialogRef<TaskAssignmentDialogComponent>);

  readonly form = this.formBuilder.group({
    assignedUserId: [this.data.task.assignedToUserId ?? ''],
  });

  submit(): void {
    const request: AssignTaskRequest = {
      assignedUserId: this.form.controls.assignedUserId.value || null,
    };
    this.dialogRef.close(request);
  }
}
