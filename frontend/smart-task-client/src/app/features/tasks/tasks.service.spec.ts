import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../core/models/api-response.model';
import { PagedResponse } from '../../core/models/pagination.model';
import { TaskResponse } from '../../core/models/task.model';
import { TasksService } from './tasks.service';

describe('TasksService', () => {
  let service: TasksService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(TasksService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('sends task filters, sorting and pagination to the project task endpoint', () => {
    service
      .list('project-1', {
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
      })
      .subscribe();

    const request = httpTesting.expectOne(
      (candidate) => candidate.url === `${environment.apiBaseUrl}/projects/project-1/tasks`,
    );

    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('keyword')).toBe('login');
    expect(request.request.params.get('status')).toBe('1');
    expect(request.request.params.get('priority')).toBe('2');
    expect(request.request.params.get('assignedUserId')).toBe('member-1');
    expect(request.request.params.get('dueDateFrom')).toBe('2026-08-01');
    expect(request.request.params.get('dueDateTo')).toBe('2026-08-31');
    expect(request.request.params.get('pageNumber')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('25');
    expect(request.request.params.get('sortColumn')).toBe('dueDate');
    expect(request.request.params.get('sortDirection')).toBe('asc');
    request.flush(successResponse(emptyPage()));
  });

  it('uses the assignment, status, priority and delete endpoints', () => {
    const taskId = 'task-1';

    service.assign(taskId, { assignedUserId: 'member-1' }).subscribe();
    const assignmentRequest = httpTesting.expectOne(
      `${environment.apiBaseUrl}/tasks/${taskId}/assignment`,
    );
    expect(assignmentRequest.request.method).toBe('PATCH');
    expect(assignmentRequest.request.body).toEqual({ assignedUserId: 'member-1' });
    assignmentRequest.flush(successResponse(null));

    service.updateStatus(taskId, { status: 1 }).subscribe();
    const statusRequest = httpTesting.expectOne(`${environment.apiBaseUrl}/tasks/${taskId}/status`);
    expect(statusRequest.request.body).toEqual({ status: 1 });
    statusRequest.flush(successResponse(null));

    service.updatePriority(taskId, { priority: 3 }).subscribe();
    const priorityRequest = httpTesting.expectOne(
      `${environment.apiBaseUrl}/tasks/${taskId}/priority`,
    );
    expect(priorityRequest.request.body).toEqual({ priority: 3 });
    priorityRequest.flush(successResponse(null));

    service.delete(taskId).subscribe();
    const deleteRequest = httpTesting.expectOne(`${environment.apiBaseUrl}/tasks/${taskId}`);
    expect(deleteRequest.request.method).toBe('DELETE');
    deleteRequest.flush(successResponse(null));
  });
});

function successResponse<T>(data: T): ApiResponse<T> {
  return {
    success: true,
    message: 'Success',
    data,
    errors: null,
    traceId: 'test-trace-id',
  };
}

function emptyPage(): PagedResponse<TaskResponse> {
  return {
    items: [],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
  };
}
