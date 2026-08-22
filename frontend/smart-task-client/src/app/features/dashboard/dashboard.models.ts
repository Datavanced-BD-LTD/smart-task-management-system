import { ApiResponse } from '../../core/models/api-response.model';

export type DashboardTaskStatus = 0 | 1 | 2 | 3;
export type DashboardTaskPriority = 0 | 1 | 2 | 3;

export interface DashboardStatusCount {
  readonly status: DashboardTaskStatus;
  readonly count: number;
}

export interface DashboardPriorityCount {
  readonly priority: DashboardTaskPriority;
  readonly count: number;
}

export interface DashboardSummaryResponse {
  readonly totalProjects: number;
  readonly totalTasks: number;
  readonly tasksByStatus: readonly DashboardStatusCount[];
  readonly tasksByPriority: readonly DashboardPriorityCount[];
  readonly completedTaskCount: number;
  readonly pendingTaskCount: number;
  readonly upcomingDueTaskCount: number;
}

export type DashboardSummaryApiResponse = ApiResponse<DashboardSummaryResponse>;

export interface DashboardMetricRow {
  readonly key: number;
  readonly label: string;
  readonly count: number;
  readonly className: string;
}

export const DASHBOARD_STATUS_DEFINITIONS = [
  { key: 0, label: 'To Do', className: 'status-todo' },
  { key: 1, label: 'In Progress', className: 'status-in-progress' },
  { key: 2, label: 'Completed', className: 'status-completed' },
  { key: 3, label: 'Cancelled', className: 'status-cancelled' },
] as const;

export const DASHBOARD_PRIORITY_DEFINITIONS = [
  { key: 0, label: 'Low', className: 'priority-low' },
  { key: 1, label: 'Medium', className: 'priority-medium' },
  { key: 2, label: 'High', className: 'priority-high' },
  { key: 3, label: 'Critical', className: 'priority-critical' },
] as const;
