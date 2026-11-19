import { Injectable, signal } from '@angular/core';

export interface Notification {
  id: number;
  message: string;
  kind: 'success' | 'error' | 'info';
}

let nextId = 1;

/** App-wide toast notifications, rendered by <app-toast-host> in the root shell. */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  readonly notifications = signal<Notification[]>([]);

  success(message: string): void {
    this.push(message, 'success');
  }

  error(message: string): void {
    this.push(message, 'error');
  }

  info(message: string): void {
    this.push(message, 'info');
  }

  dismiss(id: number): void {
    this.notifications.update((list) => list.filter((n) => n.id !== id));
  }

  private push(message: string, kind: Notification['kind']): void {
    const id = nextId++;
    this.notifications.update((list) => [...list, { id, message, kind }]);
    setTimeout(() => this.dismiss(id), kind === 'error' ? 8000 : 4500);
  }
}
