import { DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subscription, finalize, timer } from 'rxjs';
import { ContentService } from '../../../core/services/content.service';
import { SchedulesService } from '../../../core/services/schedules.service';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ContentDetailResponse } from '../../../core/models/content.model';
import { StatusBadge } from '../../../shared/status-badge/status-badge';

const POLL_INTERVAL_MS = 5000;
const IN_FLIGHT_STATUSES = new Set(['Pending', 'Running']);

@Component({
  selector: 'app-content-detail',
  imports: [RouterLink, ReactiveFormsModule, StatusBadge, DatePipe],
  templateUrl: './content-detail.html',
  styleUrl: './content-detail.scss',
})
export class ContentDetail implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly contentService = inject(ContentService);
  private readonly schedulesService = inject(SchedulesService);
  private readonly notifications = inject(NotificationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly auth = inject(AuthService);

  readonly detail = signal<ContentDetailResponse | null>(null);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly showRejectForm = signal(false);
  readonly editingOptimized = signal(false);
  readonly showScheduleForm = signal(false);

  readonly rejectForm = this.fb.nonNullable.group({
    reason: ['', [Validators.required, Validators.maxLength(2000)]],
  });
  readonly editForm = this.fb.nonNullable.group({
    editedContent: ['', [Validators.required, Validators.maxLength(5000)]],
  });
  readonly scheduleForm = this.fb.nonNullable.group({
    scheduledAt: ['', [Validators.required]],
  });

  private executionId!: string;
  private pollSub?: Subscription;

  ngOnInit(): void {
    this.executionId = this.route.snapshot.paramMap.get('id')!;
    this.load(true);
  }

  ngOnDestroy(): void {
    this.pollSub?.unsubscribe();
  }

  private load(initial = false): void {
    if (initial) this.loading.set(true);
    this.contentService.getById(this.executionId).subscribe({
      next: (detail) => {
        this.detail.set(detail);
        this.loading.set(false);
        if (!this.editingOptimized()) {
          this.editForm.patchValue({ editedContent: detail.optimizedContent ?? '' });
        }
        this.schedulePolling(detail.status);
      },
      error: () => this.loading.set(false),
    });
  }

  private schedulePolling(status: string): void {
    this.pollSub?.unsubscribe();
    if (IN_FLIGHT_STATUSES.has(status)) {
      this.pollSub = timer(POLL_INTERVAL_MS).subscribe(() => this.load());
    }
  }

  approve(): void {
    this.busy.set(true);
    this.contentService
      .approve(this.executionId)
      .pipe(finalize(() => this.busy.set(false)))
      .subscribe({
        next: () => {
          this.notifications.success('Content approved.');
          this.load();
        },
      });
  }

  submitReject(): void {
    if (this.rejectForm.invalid) {
      this.rejectForm.markAllAsTouched();
      return;
    }
    this.busy.set(true);
    this.contentService
      .reject(this.executionId, this.rejectForm.getRawValue())
      .pipe(finalize(() => this.busy.set(false)))
      .subscribe({
        next: () => {
          this.notifications.success('Content rejected.');
          this.showRejectForm.set(false);
          this.load();
        },
      });
  }

  submitEdit(): void {
    if (this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }
    this.busy.set(true);
    this.contentService
      .edit(this.executionId, this.editForm.getRawValue())
      .pipe(finalize(() => this.busy.set(false)))
      .subscribe({
        next: () => {
          this.notifications.success('Final copy updated.');
          this.editingOptimized.set(false);
          this.load();
        },
      });
  }

  regenerate(): void {
    if (!confirm('Regenerate this topic? This creates a new, separate execution and runs the full pipeline again.')) {
      return;
    }
    this.busy.set(true);
    this.contentService
      .regenerate(this.executionId)
      .pipe(finalize(() => this.busy.set(false)))
      .subscribe({
        next: (result) => this.router.navigate(['/content', result.executionId]),
      });
  }

  submitSchedule(): void {
    const postId = this.detail()?.linkedInPostId;
    if (this.scheduleForm.invalid || !postId) {
      this.scheduleForm.markAllAsTouched();
      return;
    }
    const scheduledAtUtc = new Date(this.scheduleForm.getRawValue().scheduledAt).toISOString();

    this.busy.set(true);
    this.schedulesService
      .create({ linkedInPostId: postId, scheduledAtUtc })
      .pipe(finalize(() => this.busy.set(false)))
      .subscribe({
        next: () => {
          this.notifications.success('Publishing scheduled.');
          this.showScheduleForm.set(false);
          this.load();
        },
      });
  }
}
