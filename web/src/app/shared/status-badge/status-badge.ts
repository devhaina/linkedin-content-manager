import { Component, computed, input } from '@angular/core';

type BadgeKind = 'neutral' | 'success' | 'danger' | 'warning' | 'info';

// Every status string across Campaign/ExecutionStatus/ApprovalStatus/ScheduleStatus maps here —
// shared because several of these names (e.g. "Pending", "Failed", "Cancelled") recur across
// different enums with the same intended meaning.
const KIND_BY_STATUS: Record<string, BadgeKind> = {
  Active: 'success',
  Paused: 'warning',
  Archived: 'neutral',
  Pending: 'warning',
  Running: 'info',
  Completed: 'success',
  Approved: 'success',
  Scheduled: 'info',
  Published: 'success',
  Recommended: 'neutral',
  Failed: 'danger',
  PartiallyFailed: 'danger',
  Rejected: 'danger',
  Cancelled: 'neutral',
  Draft: 'neutral',
  Critiqued: 'warning',
  Optimized: 'success',
};

@Component({
  selector: 'app-status-badge',
  template: `<span class="badge badge-{{ kind() }}">{{ status() }}</span>`,
})
export class StatusBadge {
  readonly status = input.required<string | null | undefined>();
  readonly kind = computed<BadgeKind>(() => KIND_BY_STATUS[this.status() ?? ''] ?? 'neutral');
}
