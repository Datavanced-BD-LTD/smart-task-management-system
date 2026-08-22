export interface ApiError {
  readonly code: string;
  readonly message: string;
  readonly field?: string | null;
}

export interface ApiResponse<T> {
  readonly success: boolean;
  readonly message: string;
  readonly data: T | null;
  readonly errors: readonly ApiError[] | null;
  readonly traceId: string;
}
