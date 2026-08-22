export interface PagedResponse<T> {
  readonly items: readonly T[];
  readonly pageNumber: number;
  readonly pageSize: number;
  readonly totalCount: number;
  readonly totalPages: number;
}

export interface PaginationRequest {
  readonly pageNumber: number;
  readonly pageSize: number;
}
