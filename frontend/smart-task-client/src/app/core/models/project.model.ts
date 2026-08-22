export interface ProjectResponse {
  readonly projectId: string;
  readonly name: string;
  readonly description: string | null;
  readonly projectManagerId: string;
  readonly createdByUserId: string;
  readonly createdAtUtc: string;
  readonly updatedAtUtc: string;
}

export interface CreateProjectRequest {
  readonly name: string;
  readonly description: string | null;
  readonly projectManagerId?: string | null;
}

export type UpdateProjectRequest = CreateProjectRequest;

export interface ProjectListQuery {
  readonly search?: string;
  readonly sortBy: 'name' | 'createdAt' | 'updatedAt';
  readonly sortDirection: 'asc' | 'desc';
  readonly page: number;
  readonly pageSize: number;
}

export interface ProjectMemberResponse {
  readonly userId: string;
  readonly email: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly addedByUserId: string;
  readonly addedAtUtc: string;
}

export interface AddProjectMemberRequest {
  readonly userId: string;
}
