import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../core/models/api-response.model';
import { PagedResponse } from '../../core/models/pagination.model';
import {
  AddProjectMemberRequest,
  CreateProjectRequest,
  ProjectListQuery,
  ProjectMemberResponse,
  ProjectResponse,
  UpdateProjectRequest,
} from '../../core/models/project.model';

@Injectable({ providedIn: 'root' })
export class ProjectsService {
  private readonly http = inject(HttpClient);
  private readonly projectsUrl = `${environment.apiBaseUrl}/v1/projects`;

  list(query: ProjectListQuery): Observable<ApiResponse<PagedResponse<ProjectResponse>>> {
    let params = new HttpParams()
      .set('sortBy', query.sortBy)
      .set('sortDirection', query.sortDirection)
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    const search = query.search?.trim();

    if (search) {
      params = params.set('search', search);
    }

    return this.http.get<ApiResponse<PagedResponse<ProjectResponse>>>(this.projectsUrl, {
      params,
    });
  }

  getById(projectId: string): Observable<ApiResponse<ProjectResponse>> {
    return this.http.get<ApiResponse<ProjectResponse>>(`${this.projectsUrl}/${projectId}`);
  }

  create(request: CreateProjectRequest): Observable<ApiResponse<ProjectResponse>> {
    return this.http.post<ApiResponse<ProjectResponse>>(this.projectsUrl, request);
  }

  update(
    projectId: string,
    request: UpdateProjectRequest,
  ): Observable<ApiResponse<ProjectResponse>> {
    return this.http.put<ApiResponse<ProjectResponse>>(`${this.projectsUrl}/${projectId}`, request);
  }

  delete(projectId: string): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.projectsUrl}/${projectId}`);
  }

  listMembers(projectId: string): Observable<ApiResponse<readonly ProjectMemberResponse[]>> {
    return this.http.get<ApiResponse<readonly ProjectMemberResponse[]>>(
      `${this.projectsUrl}/${projectId}/members`,
    );
  }

  addMember(
    projectId: string,
    request: AddProjectMemberRequest,
  ): Observable<ApiResponse<ProjectMemberResponse>> {
    return this.http.post<ApiResponse<ProjectMemberResponse>>(
      `${this.projectsUrl}/${projectId}/members`,
      request,
    );
  }

  removeMember(projectId: string, userId: string): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(
      `${this.projectsUrl}/${projectId}/members/${userId}`,
    );
  }
}
