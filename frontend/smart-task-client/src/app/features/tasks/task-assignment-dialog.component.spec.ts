import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { ProjectMemberResponse } from '../../core/models/project.model';
import {
  TaskAssignmentDialogComponent,
  TaskAssignmentDialogData,
} from './task-assignment-dialog.component';

describe('TaskAssignmentDialogComponent', () => {
  let fixture: ComponentFixture<TaskAssignmentDialogComponent>;
  let component: TaskAssignmentDialogComponent;
  let close: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    close = vi.fn();
    const data: TaskAssignmentDialogData = {
      task: {
        id: 'task-1',
        projectId: 'project-1',
        title: 'Task',
        description: null,
        assignedToUserId: null,
        createdByUserId: 'manager-1',
        status: 0,
        priority: 1,
        dueDate: null,
        createdAtUtc: '2026-08-01T00:00:00Z',
        updatedAtUtc: null,
      },
      members: [createMember()],
    };

    await TestBed.configureTestingModule({
      imports: [TaskAssignmentDialogComponent],
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: data },
        { provide: MatDialogRef, useValue: { close } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TaskAssignmentDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('offers project members and supports unassignment', () => {
    expect(component.data.members).toEqual([createMember()]);

    component.submit();

    expect(close).toHaveBeenCalledWith({ assignedUserId: null });
  });
});

function createMember(): ProjectMemberResponse {
  return {
    userId: 'member-1',
    email: 'member@example.com',
    firstName: 'Member',
    lastName: 'One',
    addedByUserId: 'manager-1',
    addedAtUtc: '2026-08-01T00:00:00Z',
  };
}
