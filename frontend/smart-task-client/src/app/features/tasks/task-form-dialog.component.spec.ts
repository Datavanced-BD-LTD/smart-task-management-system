import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { ProjectMemberResponse } from '../../core/models/project.model';
import { TaskFormDialogComponent, TaskFormDialogData } from './task-form-dialog.component';

describe('TaskFormDialogComponent', () => {
  let fixture: ComponentFixture<TaskFormDialogComponent>;
  let component: TaskFormDialogComponent;
  let close: ReturnType<typeof vi.fn>;

  const members = [createMember()];

  beforeEach(async () => {
    close = vi.fn();
    const data: TaskFormDialogData = { members };

    await TestBed.configureTestingModule({
      imports: [TaskFormDialogComponent],
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: data },
        { provide: MatDialogRef, useValue: { close } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TaskFormDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('requires a task title before submitting', () => {
    component.submit();

    expect(component.form.invalid).toBe(true);
    expect(close).not.toHaveBeenCalled();
  });

  it('loads existing task data and submits typed values', () => {
    const task = {
      id: 'task-1',
      projectId: 'project-1',
      title: 'Existing task',
      description: 'Existing description',
      assignedToUserId: 'member-1',
      createdByUserId: 'manager-1',
      status: 1 as const,
      priority: 2 as const,
      dueDate: '2026-08-30T00:00:00Z',
      createdAtUtc: '2026-08-01T00:00:00Z',
      updatedAtUtc: null,
    };
    const existingData: TaskFormDialogData = { task, members };

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [TaskFormDialogComponent],
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: existingData },
        { provide: MatDialogRef, useValue: { close } },
      ],
    });
    const existingFixture = TestBed.createComponent(TaskFormDialogComponent);
    const existingComponent = existingFixture.componentInstance;
    existingFixture.detectChanges();

    expect(existingComponent.form.getRawValue()).toMatchObject({
      title: 'Existing task',
      assignedToUserId: 'member-1',
      status: 1,
      priority: 2,
      dueDate: '2026-08-30',
    });
    expect(existingComponent.members).toEqual(members);

    existingComponent.submit();

    expect(close).toHaveBeenCalledWith({
      title: 'Existing task',
      description: 'Existing description',
      assignedToUserId: 'member-1',
      status: 1,
      priority: 2,
      dueDate: '2026-08-30',
    });
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
