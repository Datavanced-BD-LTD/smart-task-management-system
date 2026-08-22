import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../core/models/api-response.model';
import { ProjectsService } from './projects.service';

describe('ProjectsService', () => {
  let service: ProjectsService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(ProjectsService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('sends the exact project search, sorting and pagination parameters', () => {
    service
      .list({
        search: 'alpha',
        sortBy: 'name',
        sortDirection: 'asc',
        page: 2,
        pageSize: 50,
      })
      .subscribe();

    const request = httpTesting.expectOne(
      (candidate) => candidate.url === `${environment.apiBaseUrl}/v1/projects`,
    );

    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('search')).toBe('alpha');
    expect(request.request.params.get('sortBy')).toBe('name');
    expect(request.request.params.get('sortDirection')).toBe('asc');
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('50');
    request.flush(successResponse(null));
  });

  it('omits the search parameter when no search text is provided', () => {
    service
      .list({
        search: undefined,
        sortBy: 'createdAt',
        sortDirection: 'desc',
        page: 1,
        pageSize: 20,
      })
      .subscribe();

    const request = httpTesting.expectOne(
      (candidate) => candidate.url === `${environment.apiBaseUrl}/v1/projects`,
    );

    expect(request.request.params.has('search')).toBe(false);
    request.flush(successResponse(null));
  });

  it('uses the project membership endpoints for list, add and remove', () => {
    const projectId = 'project-1';
    const userId = 'user-1';

    service.listMembers(projectId).subscribe();
    httpTesting
      .expectOne(`${environment.apiBaseUrl}/v1/projects/${projectId}/members`)
      .flush(successResponse(null));

    service.addMember(projectId, { userId }).subscribe();
    const addRequest = httpTesting.expectOne(
      `${environment.apiBaseUrl}/v1/projects/${projectId}/members`,
    );
    expect(addRequest.request.method).toBe('POST');
    expect(addRequest.request.body).toEqual({ userId });
    addRequest.flush(successResponse(null));

    service.removeMember(projectId, userId).subscribe();
    const removeRequest = httpTesting.expectOne(
      `${environment.apiBaseUrl}/v1/projects/${projectId}/members/${userId}`,
    );
    expect(removeRequest.request.method).toBe('DELETE');
    removeRequest.flush(successResponse(null));
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
