import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RefreshTokenRequest, RegisterRequest } from '../models/auth.model';
import { UserRole } from '../models/enums.model';

const STORAGE_KEY = 'licm.auth';

interface StoredSession {
  accessToken: string;
  refreshToken: string;
  userId: string;
  email: string;
  displayName: string;
  role: UserRole;
}

/**
 * Owns the JWT/refresh-token lifecycle. Session is persisted to localStorage so a page
 * refresh doesn't log the user out — the AuthInterceptor reads the current access token from
 * here on every request, and the ErrorInterceptor calls refresh()/logout() on 401s.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly session = signal<StoredSession | null>(this.readStoredSession());

  readonly isAuthenticated = computed(() => this.session() !== null);
  readonly currentUser = computed(() => this.session());
  readonly role = computed(() => this.session()?.role ?? null);
  readonly canEdit = computed(() => {
    const r = this.role();
    return r === UserRole.Admin || r === UserRole.ContentManager;
  });

  constructor(private readonly http: HttpClient) {}

  get accessToken(): string | null {
    return this.session()?.accessToken ?? null;
  }

  get refreshTokenValue(): string | null {
    return this.session()?.refreshToken ?? null;
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}/auth/register`, request)
      .pipe(tap((response) => this.storeSession(response)));
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}/auth/login`, request)
      .pipe(tap((response) => this.storeSession(response)));
  }

  refresh(): Observable<AuthResponse> {
    const request: RefreshTokenRequest = { refreshToken: this.refreshTokenValue ?? '' };
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}/auth/refresh`, request)
      .pipe(tap((response) => this.storeSession(response)));
  }

  logout(): void {
    this.session.set(null);
    localStorage.removeItem(STORAGE_KEY);
  }

  private storeSession(response: AuthResponse): void {
    const stored: StoredSession = {
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
      userId: response.userId,
      email: response.email,
      displayName: response.displayName,
      role: response.role,
    };
    this.session.set(stored);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(stored));
  }

  private readStoredSession(): StoredSession | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw ? (JSON.parse(raw) as StoredSession) : null;
    } catch {
      return null;
    }
  }
}
