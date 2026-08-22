import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { finalize } from 'rxjs';
import { ApiError } from '../../core/models/api-response.model';
import { ApiErrorService } from '../../core/services/api-error.service';
import {
  DASHBOARD_PRIORITY_DEFINITIONS,
  DASHBOARD_STATUS_DEFINITIONS,
  DashboardMetricRow,
  DashboardSummaryResponse,
} from './dashboard.models';
import { DashboardService } from './dashboard.service';

@Component({
  imports: [MatButtonModule, MatCardModule, MatProgressBarModule],
  selector: 'app-dashboard-page',
  styleUrl: './dashboard-page.component.scss',
  templateUrl: './dashboard-page.component.html',
})
export class DashboardPageComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  private readonly apiErrorService = inject(ApiErrorService);

  readonly summary = signal<DashboardSummaryResponse | null>(null);
  readonly loading = signal(false);
  readonly hasLoaded = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly apiErrors = signal<readonly ApiError[]>([]);
  readonly upcomingDays = 7;

  readonly statusRows = computed<readonly DashboardMetricRow[]>(() => {
    const summary = this.summary();

    return DASHBOARD_STATUS_DEFINITIONS.map((definition) => ({
      ...definition,
      count: summary?.tasksByStatus.find((item) => item.status === definition.key)?.count ?? 0,
    }));
  });

  readonly priorityRows = computed<readonly DashboardMetricRow[]>(() => {
    const summary = this.summary();

    return DASHBOARD_PRIORITY_DEFINITIONS.map((definition) => ({
      ...definition,
      count: summary?.tasksByPriority.find((item) => item.priority === definition.key)?.count ?? 0,
    }));
  });

  readonly maxStatusCount = computed(() => this.getMaximum(this.statusRows()));
  readonly maxPriorityCount = computed(() => this.getMaximum(this.priorityRows()));

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.apiErrors.set([]);

    this.dashboardService
      .getSummary(this.upcomingDays)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          this.hasLoaded.set(true);

          if (response.success && response.data) {
            this.summary.set(response.data);
            return;
          }

          this.errorMessage.set(response.message || 'Dashboard data could not be loaded.');
        },
        error: (error: unknown) => {
          this.hasLoaded.set(true);
          this.errorMessage.set(this.apiErrorService.getMessage(error));
          this.apiErrors.set(this.apiErrorService.getErrors(error));
        },
      });
  }

  refreshDashboard(): void {
    this.loadDashboard();
  }

  getBarWidth(count: number, maximum: number): number {
    return maximum > 0 ? (count / maximum) * 100 : 0;
  }

  private getMaximum(rows: readonly DashboardMetricRow[]): number {
    return Math.max(...rows.map((row) => row.count), 1);
  }
}
