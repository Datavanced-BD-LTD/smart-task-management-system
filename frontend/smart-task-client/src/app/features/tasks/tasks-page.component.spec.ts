import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ApiErrorService } from '../../core/services/api-error.service';
import { AuthService } from '../../core/services/auth.service';
import { PagedResponse } from '../../core/models/pagination.model';
import { ProjectMemberResponse, ProjectResponse } from '../../core/models/project.model';
import { TaskResponse } from '../../core/models/task.model';
import { ProjectConfirmationDialogComponent } from '../projects/project-confirmation-dialog.component';
import { ProjectsService } from '../projects/projects.service';
import { TasksPageComponent } from './tasks-page.component';
import { TasksService } from './tasks.service';

type MockFunction = ReturnType<typeof vi.fn>;

interface TasksServiceMock {
  list: MockFunction;
  create: MockFunction;
  update: MockFunction;
  delete: MockFunction;
  assign: MockFunction;
  updateStatus: MockFunction;
  updatePriority: MockFunction;
}

interface ProjectsServiceMock {
  getById: MockFunction;
  listMembers: MockFunction;
}

interface ApiErrorServiceMock {
  getMessage: MockFunction;
  getErrors: MockFunction;
}

interface AuthServiceMock {
  currentRoles: MockFunction;
  currentUser: MockFunction;
}

describe('TasksPageComponent', () => {
  let fixture: ComponentFixture<TasksPageComponent>;
  let component: TasksPageComponent;
  let tasksService: TasksServiceMock;
  let projectsService: ProjectsServiceMock;
  let apiErrorService: ApiErrorServiceMock;
  let authService: AuthServiceMock;
  let dialog: { open: ReturnType<typeof vi.fn> };

  const project = createProject();
  const member = createMember();
  const task = createTask();

  beforeEach(async () => {
    tasksService = {
      list: vi.fn(() => of(successResponse(successPage([task])))),
      create: vi.fn(() => of(successResponse(task))),
      update: vi.fn(() => of(successResponse(task))),
      delete: vi.fn(() => of(successResponse(null))),
      assign: vi.fn(() => of(successResponse(task))),
      updateStatus: vi.fn(() => of(successResponse({ ...task, status: 1 as const }))),
      updatePriority: vi.fn(() => of(successResponse({ ...task, priority: 3 as const }))),
    };
    projectsService = {
      getById: vi.fn(() => of(successResponse(project))),
      listMembers: vi.fn(() => of(successResponse([member]))),
    };
    apiErrorService = {
      getMessage: vi.fn(() => 'Unable to load tasks.'),
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
      imports: [TasksPageComponent],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => 'project-1' } } },
        },
        { provide: TasksService, useValue: tasksService },
        { provide: ProjectsService, useValue: projectsService },
        { provide: ApiErrorService, useValue: apiErrorService },
        { provide: AuthService, useValue: authService },
        { provide: MatDialog, useValue: dialog },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TasksPageComponent);
    component = fixture.componentInstance;
  });

  it('loads the selected project, members and tasks', () => {
    fixture.detectChanges();

    expect(projectsService.getById).toHaveBeenCalledWith('project-1');
    expect(projectsService.listMembers).toHaveBeenCalledWith('project-1');
    expect(tasksService.list).toHaveBeenCalledWith(
      'project-1',
      expect.objectContaining({ pageNumber: 1, pageSize: 10 }),
    );
    expect(component.memberName('member-1')).toBe('Member One');
    expect(fixture.nativeElement.textContent).toContain('Prepare login');
  });

  it('sends search, filters, sorting and pagination to the API', () => {
    fixture.detectChanges();

    component.filtersForm.patchValue({
      keyword: 'login',
      status: 1,
      priority: 2,
      assignedUserId: 'member-1',
      dueDateFrom: new Date(2026, 7, 1),
      dueDateTo: new Date(2026, 7, 31),
    });
    component.onSort({ active: 'dueDate', direction: 'asc' });
    component.onPageChange({ pageIndex: 1, pageSize: 25, length: 100 });

    expect(tasksService.list).toHaveBeenLastCalledWith('project-1', {
      keyword: 'login',
      status: 1,
      priority: 2,
      assignedUserId: 'member-1',
      dueDateFrom: '2026-08-01',
      dueDateTo: '2026-08-31',
      pageNumber: 2,
      pageSize: 25,
      sortColumn: 'dueDate',
      sortDirection: 'asc',
    });
  });

  it('renders an empty task state', () => {
    tasksService.list.mockReturnValue(of(successResponse(successPage([]))));

    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No tasks found');
  });

  it('displays safe API errors', () => {
    tasksService.list.mockReturnValue(throwError(() => new Error('secret backend detail')));
    apiErrorService.getMessage.mockReturnValue('Unable to load tasks.');

    fixture.detectChanges();

    expect(component.errorMessage()).toBe('Unable to load tasks.');
    expect(fixture.nativeElement.textContent).not.toContain('secret backend detail');
  });

  it('uses project members in assignment dialogs and supports delete confirmation', () => {
    fixture.detectChanges();
    component.openAssignmentDialog(task);

    expect(dialog.open).toHaveBeenCalledWith(
      expect.anything(),
      expect.objectContaining({
        data: expect.objectContaining({ members: [member] }),
      }),
    );

    dialog.open.mockReturnValue({ afterClosed: () => of(true) });
    component.confirmDelete(task);

    expect(dialog.open).toHaveBeenCalledWith(ProjectConfirmationDialogComponent, expect.anything());
    expect(tasksService.delete).toHaveBeenCalledWith('task-1');
  });

  it('updates status and priority through their dedicated endpoints', () => {
    fixture.detectChanges();

    component.onStatusChange(task, 1);
    component.onPriorityChange(task, 3);

    expect(tasksService.updateStatus).toHaveBeenCalledWith('task-1', { status: 1 });
    expect(tasksService.updatePriority).toHaveBeenCalledWith('task-1', { priority: 3 });
  });

  it('hides management actions and limits team members to assigned status updates', () => {
    authService.currentRoles.mockReturnValue(['TeamMember']);
    authService.currentUser.mockReturnValue({ userId: 'member-1' });
    fixture.detectChanges();

    expect(component.canManageTasks()).toBe(false);
    expect(component.canUpdateStatus(task)).toBe(true);
    expect(component.canUpdateStatus({ ...task, assignedToUserId: 'other-member' })).toBe(false);

    component.openAssignmentDialog(task);
    component.confirmDelete(task);

    expect(dialog.open).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).not.toContain('Create task');
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

function successPage(items: readonly TaskResponse[]): PagedResponse<TaskResponse> {
  return {
    items,
    pageNumber: 1,
    pageSize: 10,
    totalCount: items.length,
    totalPages: items.length ? 1 : 0,
  };
}

function createProject(): ProjectResponse {
  return {
    projectId: 'project-1',
    name: 'Alpha project',
    description: 'Project description',
    projectManagerId: 'manager-1',
    createdByUserId: 'admin-1',
    createdAtUtc: '2026-08-01T00:00:00Z',
    updatedAtUtc: '2026-08-01T00:00:00Z',
  };
}

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

function createTask(): TaskResponse {
  return {
    id: 'task-1',
    projectId: 'project-1',
    title: 'Prepare login',
    description: 'Prepare the login page',
    assignedToUserId: 'member-1',
    createdByUserId: 'manager-1',
    status: 0,
    priority: 1,
    dueDate: '2026-08-30T00:00:00Z',
    createdAtUtc: '2026-08-01T00:00:00Z',
    updatedAtUtc: null,
  };
}
