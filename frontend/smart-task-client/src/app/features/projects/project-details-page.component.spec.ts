import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ApiErrorService } from '../../core/services/api-error.service';
import { AuthService } from '../../core/services/auth.service';
import { ProjectMemberResponse, ProjectResponse } from '../../core/models/project.model';
import { ProjectDetailsPageComponent } from './project-details-page.component';
import { ProjectsService } from './projects.service';

describe('ProjectDetailsPageComponent', () => {
  let fixture: ComponentFixture<ProjectDetailsPageComponent>;
  let component: ProjectDetailsPageComponent;
  let projectsService: {
    getById: ReturnType<typeof vi.fn>;
    listMembers: ReturnType<typeof vi.fn>;
    addMember: ReturnType<typeof vi.fn>;
    removeMember: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
  };
  let apiErrorService: {
    getMessage: ReturnType<typeof vi.fn>;
    getErrors: ReturnType<typeof vi.fn>;
  };
  let authService: {
    currentRoles: ReturnType<typeof vi.fn>;
    currentUser: ReturnType<typeof vi.fn>;
  };
  let dialog: { open: ReturnType<typeof vi.fn> };

  const project = createProject();
  const member = createMember();

  beforeEach(async () => {
    projectsService = {
      getById: vi.fn(() => of(successResponse(project))),
      listMembers: vi.fn(() => of(successResponse([member]))),
      addMember: vi.fn(() => of(successResponse(member))),
      removeMember: vi.fn(() => of(successResponse(null))),
      update: vi.fn(() => of(successResponse(project))),
      delete: vi.fn(() => of(successResponse(null))),
    };
    apiErrorService = {
      getMessage: vi.fn(() => 'Project operation failed.'),
      getErrors: vi.fn(() => []),
    };
    authService = {
      currentRoles: vi.fn(() => ['Admin']),
      currentUser: vi.fn(() => ({ userId: 'admin-1' })),
    };
    dialog = {
      open: vi.fn(() => ({ afterClosed: () => of(null) })),
    };

    await TestBed.configureTestingModule({
      imports: [ProjectDetailsPageComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => project.projectId } } },
        },
        { provide: ProjectsService, useValue: projectsService },
        { provide: ApiErrorService, useValue: apiErrorService },
        { provide: AuthService, useValue: authService },
        { provide: MatDialog, useValue: dialog },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ProjectDetailsPageComponent);
    component = fixture.componentInstance;
  });

  it('loads project details and project members', () => {
    fixture.detectChanges();

    expect(projectsService.getById).toHaveBeenCalledWith(project.projectId);
    expect(projectsService.listMembers).toHaveBeenCalledWith(project.projectId);
    expect(component.project()?.name).toBe('Alpha project');
    expect(fixture.nativeElement.textContent).toContain('member@example.com');
    expect(fixture.nativeElement.textContent).toContain('Project Manager Name');
    expect(fixture.nativeElement.textContent).toContain('Team Member');
  });

  it('adds a member after the add dialog returns a request', () => {
    fixture.detectChanges();
    dialog.open.mockReturnValue({
      afterClosed: () => of({ userId: 'member-2' }),
    });

    component.openAddMemberDialog();

    expect(projectsService.addMember).toHaveBeenCalledWith(project.projectId, {
      userId: 'member-2',
    });
  });

  it('removes a member after confirmation', () => {
    fixture.detectChanges();
    dialog.open.mockReturnValue({ afterClosed: () => of(true) });

    component.confirmRemoveMember(member);

    expect(projectsService.removeMember).toHaveBeenCalledWith(project.projectId, member.userId);
  });

  it('displays membership errors safely', () => {
    fixture.detectChanges();
    projectsService.listMembers.mockReturnValue(
      throwError(() => new Error('private backend error')),
    );
    apiErrorService.getMessage.mockReturnValue('Members are unavailable.');

    component.loadMembers();

    expect(component.memberErrorMessage()).toBe('Members are unavailable.');
    expect(fixture.nativeElement.textContent).not.toContain('private backend error');
  });

  it('does not open management dialogs for a Team Member', () => {
    authService.currentRoles.mockReturnValue(['TeamMember']);
    fixture.detectChanges();

    component.openAddMemberDialog();
    component.confirmRemoveMember(member);

    expect(component.canManage()).toBe(false);
    expect(dialog.open).not.toHaveBeenCalled();
  });
});

function successResponse<T>(data: T) {
  return {
    success: true,
    message: 'Success',
    data,
    errors: null,
    traceId: 'test-trace-id',
  };
}

function createProject(): ProjectResponse {
  return {
    projectId: 'project-1',
    name: 'Alpha project',
    description: 'Description',
    projectManagerId: 'manager-1',
    createdByUserId: 'admin-1',
    createdAtUtc: '2026-08-22T00:00:00Z',
    updatedAtUtc: '2026-08-22T00:00:00Z',
    projectManagerName: 'Project Manager Name',
    projectManagerEmail: 'manager@example.com',
  };
}

function createMember(): ProjectMemberResponse {
  return {
    userId: 'member-1',
    email: 'member@example.com',
    firstName: 'Team',
    lastName: 'Member',
    addedByUserId: 'admin-1',
    addedAtUtc: '2026-08-22T00:00:00Z',
    displayName: 'Team Member',
    role: 'TeamMember',
  };
}
