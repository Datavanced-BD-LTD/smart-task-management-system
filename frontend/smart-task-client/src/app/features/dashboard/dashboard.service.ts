import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DashboardSummaryApiResponse } from './dashboard.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);

  getSummary(upcomingDays = 7): Observable<DashboardSummaryApiResponse> {
    return this.http.get<DashboardSummaryApiResponse>(
      `${environment.apiBaseUrl}/dashboard/summary`,
      { params: { upcomingDays } },
    );
  }
}
