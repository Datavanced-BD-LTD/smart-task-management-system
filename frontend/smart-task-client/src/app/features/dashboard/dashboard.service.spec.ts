import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { DashboardService } from './dashboard.service';

describe('DashboardService', () => {
  let service: DashboardService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(DashboardService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('requests the dashboard summary with the configured upcoming period', () => {
    service.getSummary(7).subscribe();

    const request = httpTesting.expectOne(
      (candidate) => candidate.url === `${environment.apiBaseUrl}/dashboard/summary`,
    );

    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('upcomingDays')).toBe('7');
    request.flush({
      success: true,
      message: 'Dashboard summary retrieved successfully.',
      data: null,
      errors: null,
      traceId: 'test-trace-id',
    });
  });
});
