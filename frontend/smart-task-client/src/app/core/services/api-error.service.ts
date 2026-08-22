import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ApiError, ApiResponse } from '../models/api-response.model';

@Injectable({ providedIn: 'root' })
export class ApiErrorService {
  getErrors(error: unknown): readonly ApiError[] {
    if (error instanceof HttpErrorResponse) {
      const response = error.error as ApiResponse<unknown> | null;

      return response?.errors ?? [];
    }

    return [];
  }

  getMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      const response = error.error as ApiResponse<unknown> | null;
      const firstError = response?.errors?.[0]?.message;

      return firstError ?? response?.message ?? error.message ?? 'An unexpected error occurred.';
    }

    return error instanceof Error ? error.message : 'An unexpected error occurred.';
  }
}
