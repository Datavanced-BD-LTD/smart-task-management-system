import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { AdminUsersService } from './admin-users.service';

describe('AdminUsersService', () => {
  let service: AdminUsersService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AdminUsersService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('loads paginated users with the search query', () => {
    service.list({ keyword: 'maria', pageNumber: 2, pageSize: 10 }).subscribe();

    const request = httpTesting.expectOne(
      (candidate) => candidate.url === `${environment.apiBaseUrl}/v1/admin/users`,
    );
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('keyword')).toBe('maria');
    expect(request.request.params.get('pageNumber')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('10');
    request.flush({ success: true, message: 'Success', data: null, errors: null, traceId: 'test' });
  });

  it('creates users and updates roles through the admin endpoints', () => {
    service.create({
      firstName: 'Maria',
      lastName: 'Manager',
      email: 'maria@example.com',
      password: 'StrongPass1!',
      role: 'ProjectManager',
    }).subscribe();
    const createRequest = httpTesting.expectOne(`${environment.apiBaseUrl}/v1/admin/users`);
    expect(createRequest.request.method).toBe('POST');
    createRequest.flush({ success: true, message: 'Success', data: null, errors: null, traceId: 'test' });

    service.updateRole('user-1', { role: 'TeamMember' }).subscribe();
    const updateRequest = httpTesting.expectOne(`${environment.apiBaseUrl}/v1/admin/users/user-1/role`);
    expect(updateRequest.request.method).toBe('PATCH');
    expect(updateRequest.request.body).toEqual({ role: 'TeamMember' });
    updateRequest.flush({ success: true, message: 'Success', data: null, errors: null, traceId: 'test' });
  });
});
