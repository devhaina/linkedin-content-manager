export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ApiErrorResponse {
  message: string;
  traceId?: string;
  validationErrors?: Record<string, string[]>;
}
