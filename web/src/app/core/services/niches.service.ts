import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Niche } from '../models/niche.model';

@Injectable({ providedIn: 'root' })
export class NichesService {
  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<Niche[]> {
    return this.http.get<Niche[]>(`${environment.apiBaseUrl}/niches`);
  }
}
