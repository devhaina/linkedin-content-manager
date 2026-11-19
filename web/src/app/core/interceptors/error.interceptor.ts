import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, switchMap, throwError } from 'rxjs';
import { ApiErrorResponse } from '../models/common.model';
import { AuthService } from '../services/auth.service';
import { NotificationService } from '../services/notification.service';

// Shared across concurrent requests so a burst of 401s triggers exactly one refresh call,
// not one per request.
let refreshInFlight: Observable<unknown> | null = null;

/**
 * On 401: tries exactly one token refresh, then retries the original request with the new
 * token. If that also fails, logs out and sends the user to /login. Every other error gets a
 * human-readable toast built from the API's own ApiErrorResponse — never a raw stack trace,
 * since the API itself never sends one (see GlobalExceptionHandler server-side).
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const notifications = inject(NotificationService);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }

      if (error.status === 401 && !req.url.includes('/auth/') && auth.refreshTokenValue) {
        refreshInFlight ??= auth.refresh();

        return refreshInFlight.pipe(
          switchMap(() => {
            refreshInFlight = null;
            const token = auth.accessToken;
            return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
          }),
          catchError((refreshError) => {
            refreshInFlight = null;
            auth.logout();
            router.navigate(['/login']);
            return throwError(() => refreshError);
          }),
        );
      }

      if (error.status === 401) {
        auth.logout();
        router.navigate(['/login']);
      }

      notifications.error(describe(error));
      return throwError(() => error);
    }),
  );
};

function describe(error: HttpErrorResponse): string {
  const body = error.error as ApiErrorResponse | undefined;
  if (body?.validationErrors) {
    const first = Object.values(body.validationErrors)[0]?.[0];
    if (first) return first;
  }
  if (body?.message) return body.message;
  if (error.status === 0) return 'Could not reach the server. Check your connection.';
  return `Request failed (${error.status}).`;
}
