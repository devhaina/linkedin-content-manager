import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/common.model';
import {
  ApprovalActionResponse,
  ContentDetailResponse,
  ContentHistoryItemResponse,
  EditContentRequest,
  EditContentResponse,
  GenerateContentRequest,
  GenerateContentResponse,
  RegenerateContentRequest,
  RejectContentRequest,
} from '../models/content.model';
import { ExecutionStatus } from '../models/enums.model';

@Injectable({ providedIn: 'root' })
export class ContentService {
  private readonly baseUrl = `${environment.apiBaseUrl}/content`;

  constructor(private readonly http: HttpClient) {}

  generate(request: GenerateContentRequest): Observable<GenerateContentResponse> {
    return this.http.post<GenerateContentResponse>(`${this.baseUrl}/generate`, request);
  }

  getById(executionId: string): Observable<ContentDetailResponse> {
    return this.http.get<ContentDetailResponse>(`${this.baseUrl}/${executionId}`);
  }

  getHistory(options: { page?: number; pageSize?: number; campaignId?: string; status?: ExecutionStatus } = {}): Observable<PagedResult<ContentHistoryItemResponse>> {
    let params = new HttpParams()
      .set('page', options.page ?? 1)
      .set('pageSize', options.pageSize ?? 50);
    if (options.campaignId) params = params.set('campaignId', options.campaignId);
    if (options.status) params = params.set('status', options.status);

    return this.http.get<PagedResult<ContentHistoryItemResponse>>(`${this.baseUrl}/history`, { params });
  }

  regenerate(executionId: string, request: RegenerateContentRequest = {}): Observable<GenerateContentResponse> {
    return this.http.post<GenerateContentResponse>(`${this.baseUrl}/${executionId}/regenerate`, request);
  }

  approve(executionId: string): Observable<ApprovalActionResponse> {
    return this.http.post<ApprovalActionResponse>(`${this.baseUrl}/${executionId}/approve`, {});
  }

  reject(executionId: string, request: RejectContentRequest): Observable<ApprovalActionResponse> {
    return this.http.post<ApprovalActionResponse>(`${this.baseUrl}/${executionId}/reject`, request);
  }

  edit(executionId: string, request: EditContentRequest): Observable<EditContentResponse> {
    return this.http.put<EditContentResponse>(`${this.baseUrl}/${executionId}/edit`, request);
  }
}
