import { ApiResponse } from './api-response.model';
import { PagedResponse } from './pagination.model';

export type TaskStatus = 0 | 1 | 2 | 3;
export type TaskPriority = 0 | 1 | 2 | 3;

export interface TaskResponse {
  readonly id: string;
  readonly projectId: string;
  readonly title: string;
  readonly description: string | null;
  readonly assignedToUserId: string | null;
  readonly createdByUserId: string;
  readonly status: TaskStatus;
  readonly priority: TaskPriority;
  readonly dueDate: string | null;
  readonly createdAtUtc: string;
  readonly updatedAtUtc: string | null;
  readonly assignedUserName?: string | null;
  readonly assignedUserEmail?: string | null;
  readonly createdByUserName?: string | null;
  readonly createdByUserEmail?: string | null;
  readonly projectName?: string | null;
}

export interface CreateTaskRequest {
  readonly title: string;
  readonly description: string | null;
  readonly assignedToUserId: string | null;
  readonly status: TaskStatus;
  readonly priority: TaskPriority;
  readonly dueDate: string | null;
}

export type UpdateTaskRequest = CreateTaskRequest;

export interface AssignTaskRequest {
  readonly assignedUserId: string | null;
}

export interface UpdateTaskStatusRequest {
  readonly status: TaskStatus;
}

export interface UpdateTaskPriorityRequest {
  readonly priority: TaskPriority;
}

export interface TaskListQuery {
  readonly keyword?: string;
  readonly status?: TaskStatus;
  readonly priority?: TaskPriority;
  readonly assignedUserId?: string;
  readonly dueDateFrom?: string;
  readonly dueDateTo?: string;
  readonly pageNumber: number;
  readonly pageSize: number;
  readonly sortColumn: 'title' | 'status' | 'priority' | 'dueDate' | 'createdAt';
  readonly sortDirection: 'asc' | 'desc';
}

export type TaskListApiResponse = ApiResponse<PagedResponse<TaskResponse>>;

export const TASK_STATUS_DEFINITIONS = [
  { key: 0, label: 'To Do', className: 'status-todo' },
  { key: 1, label: 'In Progress', className: 'status-in-progress' },
  { key: 2, label: 'Completed', className: 'status-completed' },
  { key: 3, label: 'Cancelled', className: 'status-cancelled' },
] as const;

export const TASK_PRIORITY_DEFINITIONS = [
  { key: 0, label: 'Low', className: 'priority-low' },
  { key: 1, label: 'Medium', className: 'priority-medium' },
  { key: 2, label: 'High', className: 'priority-high' },
  { key: 3, label: 'Critical', className: 'priority-critical' },
] as const;

export function taskStatusLabel(status: TaskStatus): string {
  return (
    TASK_STATUS_DEFINITIONS.find((definition) => definition.key === status)?.label ?? 'Unknown'
  );
}

export function taskPriorityLabel(priority: TaskPriority): string {
  return (
    TASK_PRIORITY_DEFINITIONS.find((definition) => definition.key === priority)?.label ?? 'Unknown'
  );
}
