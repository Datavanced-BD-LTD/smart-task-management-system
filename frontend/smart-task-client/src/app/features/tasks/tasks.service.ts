import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../core/models/api-response.model';
import {
  AssignTaskRequest,
  CreateTaskRequest,
  TaskListApiResponse,
  TaskListQuery,
  TaskResponse,
  UpdateTaskPriorityRequest,
  UpdateTaskRequest,
  UpdateTaskStatusRequest,
} from '../../core/models/task.model';

@Injectable({ providedIn: 'root' })
export class TasksService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiBaseUrl;

  list(projectId: string, query: TaskListQuery): Observable<TaskListApiResponse> {
    let params = new HttpParams()
      .set('pageNumber', query.pageNumber)
      .set('pageSize', query.pageSize)
      .set('sortColumn', query.sortColumn)
      .set('sortDirection', query.sortDirection);

    if (query.keyword) {
      params = params.set('keyword', query.keyword);
    }
    if (query.status !== undefined) {
      params = params.set('status', query.status);
    }
    if (query.priority !== undefined) {
      params = params.set('priority', query.priority);
    }
    if (query.assignedUserId) {
      params = params.set('assignedUserId', query.assignedUserId);
    }
    if (query.dueDateFrom) {
      params = params.set('dueDateFrom', query.dueDateFrom);
    }
    if (query.dueDateTo) {
      params = params.set('dueDateTo', query.dueDateTo);
    }

    return this.http.get<TaskListApiResponse>(`${this.apiUrl}/projects/${projectId}/tasks`, {
      params,
    });
  }

  getById(taskId: string): Observable<ApiResponse<TaskResponse>> {
    return this.http.get<ApiResponse<TaskResponse>>(`${this.apiUrl}/tasks/${taskId}`);
  }

  create(projectId: string, request: CreateTaskRequest): Observable<ApiResponse<TaskResponse>> {
    return this.http.post<ApiResponse<TaskResponse>>(
      `${this.apiUrl}/projects/${projectId}/tasks`,
      request,
    );
  }

  update(taskId: string, request: UpdateTaskRequest): Observable<ApiResponse<TaskResponse>> {
    return this.http.put<ApiResponse<TaskResponse>>(`${this.apiUrl}/tasks/${taskId}`, request);
  }

  delete(taskId: string): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.apiUrl}/tasks/${taskId}`);
  }

  assign(taskId: string, request: AssignTaskRequest): Observable<ApiResponse<TaskResponse>> {
    return this.http.patch<ApiResponse<TaskResponse>>(
      `${this.apiUrl}/tasks/${taskId}/assignment`,
      request,
    );
  }

  updateStatus(
    taskId: string,
    request: UpdateTaskStatusRequest,
  ): Observable<ApiResponse<TaskResponse>> {
    return this.http.patch<ApiResponse<TaskResponse>>(
      `${this.apiUrl}/tasks/${taskId}/status`,
      request,
    );
  }

  updatePriority(
    taskId: string,
    request: UpdateTaskPriorityRequest,
  ): Observable<ApiResponse<TaskResponse>> {
    return this.http.patch<ApiResponse<TaskResponse>>(
      `${this.apiUrl}/tasks/${taskId}/priority`,
      request,
    );
  }
}
