export type TaskStatus = 'ToDo' | 'InProgress' | 'Completed' | 'Cancelled';
export type TaskPriority = 'Low' | 'Medium' | 'High' | 'Critical';

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
}
