import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CampaignResponse, CreateCampaignRequest, UpdateCampaignRequest } from '../models/campaign.model';
import { PagedResult } from '../models/common.model';
import { CampaignStatus } from '../models/enums.model';

@Injectable({ providedIn: 'root' })
export class CampaignsService {
  private readonly baseUrl = `${environment.apiBaseUrl}/campaigns`;

  constructor(private readonly http: HttpClient) {}

  getAll(options: { page?: number; pageSize?: number; nicheId?: string; status?: CampaignStatus } = {}): Observable<PagedResult<CampaignResponse>> {
    let params = new HttpParams()
      .set('page', options.page ?? 1)
      .set('pageSize', options.pageSize ?? 50);
    if (options.nicheId) params = params.set('nicheId', options.nicheId);
    if (options.status) params = params.set('status', options.status);

    return this.http.get<PagedResult<CampaignResponse>>(this.baseUrl, { params });
  }

  getById(id: string): Observable<CampaignResponse> {
    return this.http.get<CampaignResponse>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateCampaignRequest): Observable<CampaignResponse> {
    return this.http.post<CampaignResponse>(this.baseUrl, request);
  }

  update(id: string, request: UpdateCampaignRequest): Observable<CampaignResponse> {
    return this.http.put<CampaignResponse>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
