import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { of, Subject, throwError } from 'rxjs';
import { ApiErrorService } from '../../core/services/api-error.service';
import { DashboardPageComponent } from './dashboard-page.component';
import { DashboardSummaryApiResponse, DashboardSummaryResponse } from './dashboard.models';
import { DashboardService } from './dashboard.service';

describe('DashboardPageComponent', () => {
  let fixture: ComponentFixture<DashboardPageComponent>;
  let component: DashboardPageComponent;
  let dashboardService: { getSummary: ReturnType<typeof vi.fn> };
  let apiErrorService: {
    getMessage: ReturnType<typeof vi.fn>;
    getErrors: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    dashboardService = {
      getSummary: vi.fn(() => of(successResponse(createSummary()))),
    };
    apiErrorService = {
      getMessage: vi.fn(() => 'Unable to load dashboard.'),
      getErrors: vi.fn(() => []),
    };

    await TestBed.configureTestingModule({
      imports: [DashboardPageComponent],
      providers: [
        provideRouter([]),
        { provide: DashboardService, useValue: dashboardService },
        { provide: ApiErrorService, useValue: apiErrorService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardPageComponent);
    component = fixture.componentInstance;
  });

  it('loads dashboard data using the default upcoming period', () => {
    fixture.detectChanges();

    expect(dashboardService.getSummary).toHaveBeenCalledWith(7);
    expect(component.summary()?.totalProjects).toBe(4);
    expect(fixture.nativeElement.textContent).toContain('4');
  });

  it('displays a loading state while the request is pending', () => {
    const response$ = new Subject<DashboardSummaryApiResponse>();
    dashboardService.getSummary.mockReturnValue(response$.asObservable());

    fixture.detectChanges();

    expect(component.loading()).toBe(true);
    expect(fixture.nativeElement.querySelector('[role="status"]')).not.toBeNull();

    response$.next(successResponse(createSummary()));
    response$.complete();
    fixture.detectChanges();

    expect(component.loading()).toBe(false);
  });

  it('displays safe API error information', () => {
    const response = new HttpErrorResponse({
      status: 500,
      error: {
        success: false,
        message: 'Dashboard service unavailable.',
        errors: [{ code: 'SERVER_ERROR', message: 'Please try again later.' }],
      },
    });
    dashboardService.getSummary.mockReturnValue(throwError(() => response));
    apiErrorService.getErrors.mockReturnValue([
      { code: 'SERVER_ERROR', message: 'Please try again later.' },
    ]);

    fixture.detectChanges();

    expect(component.errorMessage()).toBe('Unable to load dashboard.');
    expect(fixture.nativeElement.textContent).toContain('Please try again later.');
  });

  it('renders zero values for empty status and priority groups', () => {
    dashboardService.getSummary.mockReturnValue(of(successResponse(createEmptySummary())));

    fixture.detectChanges();

    const metricCounts = fixture.nativeElement.querySelectorAll('.metric-heading strong');

    expect(metricCounts.length).toBe(8);
    expect(
      Array.from(metricCounts).every((element) => (element as Element).textContent?.trim() === '0'),
    ).toBe(true);
  });

  it('reloads dashboard data when refresh is requested', () => {
    fixture.detectChanges();

    component.refreshDashboard();

    expect(dashboardService.getSummary).toHaveBeenCalledTimes(2);
  });
});

function successResponse(data: DashboardSummaryResponse): DashboardSummaryApiResponse {
  return {
    success: true,
    message: 'Dashboard summary retrieved successfully.',
    data,
    errors: null,
    traceId: 'test-trace-id',
  };
}

function createSummary(): DashboardSummaryResponse {
  return {
    totalProjects: 4,
    totalTasks: 12,
    tasksByStatus: [
      { status: 0, count: 3 },
      { status: 1, count: 4 },
      { status: 2, count: 4 },
      { status: 3, count: 1 },
    ],
    tasksByPriority: [
      { priority: 0, count: 2 },
      { priority: 1, count: 4 },
      { priority: 2, count: 3 },
      { priority: 3, count: 3 },
    ],
    completedTaskCount: 4,
    pendingTaskCount: 7,
    upcomingDueTaskCount: 2,
  };
}

function createEmptySummary(): DashboardSummaryResponse {
  return {
    totalProjects: 0,
    totalTasks: 0,
    tasksByStatus: [],
    tasksByPriority: [],
    completedTaskCount: 0,
    pendingTaskCount: 0,
    upcomingDueTaskCount: 0,
  };
}
