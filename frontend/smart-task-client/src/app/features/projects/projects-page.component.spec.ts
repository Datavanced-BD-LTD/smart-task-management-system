import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ApiErrorService } from '../../core/services/api-error.service';
import { AuthService } from '../../core/services/auth.service';
import { PagedResponse } from '../../core/models/pagination.model';
import { ProjectResponse } from '../../core/models/project.model';
import { ProjectsPageComponent } from './projects-page.component';
import { ProjectsService } from './projects.service';

describe('ProjectsPageComponent', () => {
  let fixture: ComponentFixture<ProjectsPageComponent>;
  let component: ProjectsPageComponent;
  let projectsService: {
    list: ReturnType<typeof vi.fn>;
    create: ReturnType<typeof vi.fn>;
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

  beforeEach(async () => {
    projectsService = {
      list: vi.fn(() => of(successResponse(successPage([project])))),
      create: vi.fn(() => of({ success: true, data: project })),
      update: vi.fn(() => of({ success: true, data: project })),
      delete: vi.fn(() => of({ success: true, data: null })),
    };
    apiErrorService = {
      getMessage: vi.fn(() => 'Projects could not be loaded.'),
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
      imports: [ProjectsPageComponent],
      providers: [
        provideRouter([]),
        { provide: ProjectsService, useValue: projectsService },
        { provide: ApiErrorService, useValue: apiErrorService },
        { provide: AuthService, useValue: authService },
        { provide: MatDialog, useValue: dialog },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ProjectsPageComponent);
    component = fixture.componentInstance;
  });

  it('loads and renders projects successfully', () => {
    fixture.detectChanges();

    expect(projectsService.list).toHaveBeenCalled();
    expect(component.projects()?.items).toHaveLength(1);
    expect(fixture.nativeElement.textContent).toContain('Alpha project');
  });

  it('sends search, sorting and pagination changes to the API', () => {
    fixture.detectChanges();

    component.searchControl.setValue('alpha');
    component.searchProjects();
    component.onSort({ active: 'name', direction: 'asc' });
    component.onPageChange({ pageIndex: 1, pageSize: 50, length: 100 });

    expect(projectsService.list).toHaveBeenLastCalledWith({
      search: 'alpha',
      sortBy: 'name',
      sortDirection: 'asc',
      page: 2,
      pageSize: 50,
    });
  });

  it('prevents a native page reload when the search form is submitted', () => {
    fixture.detectChanges();
    const searchSpy = vi.spyOn(component, 'searchProjects');
    const submitEvent = new SubmitEvent('submit', { cancelable: true });

    fixture.nativeElement.querySelector('form').dispatchEvent(submitEvent);

    expect(submitEvent.defaultPrevented).toBe(true);
    expect(searchSpy).toHaveBeenCalled();
  });

  it('opens delete confirmation and deletes after confirmation', () => {
    fixture.detectChanges();
    dialog.open.mockReturnValue({ afterClosed: () => of(true) });

    component.confirmDelete(project);

    expect(dialog.open).toHaveBeenCalled();
    expect(projectsService.delete).toHaveBeenCalledWith(project.projectId);
  });

  it('displays API errors safely', () => {
    projectsService.list.mockReturnValue(throwError(() => new Error('request failed')));
    apiErrorService.getMessage.mockReturnValue('Unable to load projects.');

    component.loadProjects();

    expect(component.errorMessage()).toBe('Unable to load projects.');
    expect(fixture.nativeElement.textContent).not.toContain('request failed');
  });

  it('hides management actions and blocks them for unauthorized roles', () => {
    authService.currentRoles.mockReturnValue(['TeamMember']);
    fixture.detectChanges();

    expect(component.canCreate()).toBe(false);
    expect(component.canManage(project)).toBe(false);

    component.openCreateDialog();
    component.confirmDelete(project);

    expect(dialog.open).not.toHaveBeenCalled();
    expect(projectsService.delete).not.toHaveBeenCalled();
  });
});

function createProject(): ProjectResponse {
  return {
    projectId: 'project-1',
    name: 'Alpha project',
    description: 'Project description',
    projectManagerId: 'manager-1',
    createdByUserId: 'admin-1',
    createdAtUtc: '2026-08-22T00:00:00Z',
    updatedAtUtc: '2026-08-22T00:00:00Z',
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

function successPage(items: readonly ProjectResponse[]): PagedResponse<ProjectResponse> {
  return {
    items,
    pageNumber: 1,
    pageSize: 20,
    totalCount: items.length,
    totalPages: items.length ? 1 : 0,
  };
}
