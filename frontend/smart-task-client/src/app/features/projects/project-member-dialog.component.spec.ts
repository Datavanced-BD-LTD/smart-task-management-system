import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { of } from 'rxjs';
import {
  AvailableProjectMemberResponse,
} from '../../core/models/project.model';
import { ApiErrorService } from '../../core/services/api-error.service';
import {
  ProjectMemberDialogComponent,
  ProjectMemberDialogData,
} from './project-member-dialog.component';
import { ProjectsService } from './projects.service';

describe('ProjectMemberDialogComponent', () => {
  let fixture: ComponentFixture<ProjectMemberDialogComponent>;
  let component: ProjectMemberDialogComponent;
  let close: ReturnType<typeof vi.fn>;
  const member = createMember();

  beforeEach(async () => {
    close = vi.fn();
    const data: ProjectMemberDialogData = { projectId: 'project-1' };

    await TestBed.configureTestingModule({
      imports: [ProjectMemberDialogComponent],
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: data },
        { provide: MatDialogRef, useValue: { close } },
        {
          provide: ProjectsService,
          useValue: {
            listAvailableMembers: vi.fn(() => of(successResponse({
              items: [member],
              pageNumber: 1,
              pageSize: 20,
              totalCount: 1,
              totalPages: 1,
            }))),
          },
        },
        {
          provide: ApiErrorService,
          useValue: { getMessage: vi.fn(() => 'Members unavailable.') },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ProjectMemberDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('displays friendly member data and submits only the selected user id', () => {
    component.availableMembers.set([member]);
    component.selectMember({ option: { value: member } } as never);
    component.submit();

    expect(component.memberDisplayName(member)).toBe('Ada Lovelace');
    expect(component.roleLabel(member.role)).toBe('Team Member');
    expect(close).toHaveBeenCalledWith({ userId: 'member-1' });
  });

  it('requires a selected member', () => {
    component.submit();

    expect(component.selectionError()).toContain('Select a team member');
    expect(close).not.toHaveBeenCalled();
  });

  it('submits an exact single search result even when the option was not clicked', () => {
    component.availableMembers.set([member]);
    component.searchControl.setValue('Ada Lovelace', { emitEvent: false });
    component.submit();

    expect(close).toHaveBeenCalledWith({ userId: 'member-1' });
  });

  it('prevents the browser form submission from reloading the page', () => {
    component.availableMembers.set([member]);
    component.searchControl.setValue('Ada Lovelace', { emitEvent: false });
    const submitEvent = new Event('submit', { cancelable: true });

    component.submit(submitEvent);

    expect(submitEvent.defaultPrevented).toBe(true);
    expect(close).toHaveBeenCalledWith({ userId: 'member-1' });
  });
});

function createMember(): AvailableProjectMemberResponse {
  return {
    userId: 'member-1',
    firstName: 'Ada',
    lastName: 'Lovelace',
    displayName: 'Ada Lovelace',
    email: 'ada@example.com',
    role: 'TeamMember',
  };
}

function successResponse<T>(data: T) {
  return {
    success: true,
    message: 'Success',
    data,
    errors: null,
    traceId: 'test-trace-id',
  };
}
