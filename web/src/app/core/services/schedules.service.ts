import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/common.model';
import { ScheduleStatus } from '../models/enums.model';
import { CreateScheduleRequest, ScheduleResponse, UpdateScheduleRequest } from '../models/schedule.model';

@Injectable({ providedIn: 'root' })
export class SchedulesService {
  private readonly baseUrl = `${environment.apiBaseUrl}/schedules`;

  constructor(private readonly http: HttpClient) {}

  getAll(options: { page?: number; pageSize?: number; status?: ScheduleStatus } = {}): Observable<PagedResult<ScheduleResponse>> {
    let params = new HttpParams()
      .set('page', options.page ?? 1)
      .set('pageSize', options.pageSize ?? 50);
    if (options.status) params = params.set('status', options.status);

    return this.http.get<PagedResult<ScheduleResponse>>(this.baseUrl, { params });
  }

  create(request: CreateScheduleRequest): Observable<ScheduleResponse> {
    return this.http.post<ScheduleResponse>(this.baseUrl, request);
  }

  update(id: string, request: UpdateScheduleRequest): Observable<ScheduleResponse> {
    return this.http.put<ScheduleResponse>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
