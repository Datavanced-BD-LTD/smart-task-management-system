import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import {
  AdminUserListApiResponse,
  AdminUserListQuery,
  CreateManagedUserRequest,
  ManagedUserResponse,
  UpdateManagedUserRequest,
  UpdateManagedUserRoleRequest,
} from '../models/admin-user.model';

@Injectable({ providedIn: 'root' })
export class AdminUsersService {
  private readonly http = inject(HttpClient);
  private readonly usersUrl = `${environment.apiBaseUrl}/v1/admin/users`;

  list(query: AdminUserListQuery): Observable<AdminUserListApiResponse> {
    let params = new HttpParams()
      .set('pageNumber', query.pageNumber)
      .set('pageSize', query.pageSize);

    if (query.keyword?.trim()) {
      params = params.set('keyword', query.keyword.trim());
    }

    return this.http.get<AdminUserListApiResponse>(this.usersUrl, { params });
  }

  create(request: CreateManagedUserRequest): Observable<ApiResponse<ManagedUserResponse>> {
    return this.http.post<ApiResponse<ManagedUserResponse>>(this.usersUrl, request);
  }

  updateRole(
    userId: string,
    request: UpdateManagedUserRoleRequest,
  ): Observable<ApiResponse<ManagedUserResponse>> {
    return this.http.patch<ApiResponse<ManagedUserResponse>>(
      `${this.usersUrl}/${userId}/role`,
      request,
    );
  }

  update(
    userId: string,
    request: UpdateManagedUserRequest,
  ): Observable<ApiResponse<ManagedUserResponse>> {
    return this.http.put<ApiResponse<ManagedUserResponse>>(
      `${this.usersUrl}/${userId}`,
      request,
    );
  }

  delete(userId: string): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.usersUrl}/${userId}`);
  }
}
