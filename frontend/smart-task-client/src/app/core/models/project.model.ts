export interface ProjectResponse {
  readonly projectId: string;
  readonly name: string;
  readonly description: string | null;
  readonly projectManagerId: string;
  readonly createdByUserId: string;
  readonly createdAtUtc: string;
  readonly updatedAtUtc: string;
}
