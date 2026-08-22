export interface ProjectResponse {
  readonly projectId: string;
  readonly name: string;
  readonly description: string | null;
  readonly projectManagerId: string;
  readonly createdByUserId: string;
  readonly createdAtUtc: string;
  readonly updatedAtUtc: string;
  readonly projectManagerName?: string | null;
  readonly projectManagerEmail?: string | null;
  readonly createdByUserName?: string | null;
  readonly createdByUserEmail?: string | null;
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
  readonly displayName?: string | null;
  readonly role?: string | null;
}

export interface AddProjectMemberRequest {
  readonly userId: string;
}

export interface AvailableProjectMemberResponse {
  readonly userId: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly displayName: string;
  readonly email: string;
  readonly role: string;
}

export interface AvailableProjectMemberQuery {
  readonly keyword?: string;
  readonly pageNumber: number;
  readonly pageSize: number;
}
