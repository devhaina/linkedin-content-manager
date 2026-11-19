import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { SchedulesService } from '../../../core/services/schedules.service';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ScheduleResponse } from '../../../core/models/schedule.model';
import { StatusBadge } from '../../../shared/status-badge/status-badge';

@Component({
  selector: 'app-schedule-list',
  imports: [ReactiveFormsModule, StatusBadge, DatePipe],
  templateUrl: './schedule-list.html',
})
export class ScheduleList implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly schedulesService = inject(SchedulesService);
  private readonly notifications = inject(NotificationService);
  readonly auth = inject(AuthService);

  readonly schedules = signal<ScheduleResponse[]>([]);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly editingId = signal<string | null>(null);

  readonly rescheduleForm = this.fb.nonNullable.group({
    scheduledAt: ['', [Validators.required]],
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.schedulesService
      .getAll({ pageSize: 100 })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({ next: (result) => this.schedules.set(result.items) });
  }

  startEdit(schedule: ScheduleResponse): void {
    this.editingId.set(schedule.id);
    this.rescheduleForm.reset();
  }

  submitReschedule(schedule: ScheduleResponse): void {
    if (this.rescheduleForm.invalid) {
      this.rescheduleForm.markAllAsTouched();
      return;
    }
    const scheduledAtUtc = new Date(this.rescheduleForm.getRawValue().scheduledAt).toISOString();

    this.busy.set(true);
    this.schedulesService
      .update(schedule.id, { scheduledAtUtc })
      .pipe(finalize(() => this.busy.set(false)))
      .subscribe({
        next: () => {
          this.notifications.success('Schedule updated.');
          this.editingId.set(null);
          this.load();
        },
      });
  }

  cancel(schedule: ScheduleResponse): void {
    if (!confirm('Cancel this scheduled publish?')) return;

    this.busy.set(true);
    this.schedulesService
      .delete(schedule.id)
      .pipe(finalize(() => this.busy.set(false)))
      .subscribe({
        next: () => {
          this.notifications.success('Schedule cancelled.');
          this.load();
        },
      });
  }
}
