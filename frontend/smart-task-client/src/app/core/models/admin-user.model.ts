import { PagedResponse } from './pagination.model';
import { ApiResponse } from './api-response.model';

export type ManagedUserRole = 'ProjectManager' | 'TeamMember';

export interface ManagedUserResponse {
  readonly userId: string;
  readonly email: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly displayName: string;
  readonly roles: readonly string[];
  readonly isActive: boolean;
  readonly createdAtUtc: string;
}

export interface CreateManagedUserRequest {
  readonly email: string;
  readonly password: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly role: ManagedUserRole;
}

export interface UpdateManagedUserRoleRequest {
  readonly role: ManagedUserRole;
}

export interface AdminUserListQuery {
  readonly keyword?: string;
  readonly pageNumber: number;
  readonly pageSize: number;
}

export type AdminUserListApiResponse = ApiResponse<PagedResponse<ManagedUserResponse>>;
