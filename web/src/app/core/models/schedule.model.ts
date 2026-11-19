import { ScheduleStatus } from './enums.model';

export interface CreateScheduleRequest {
  linkedInPostId: string;
  scheduledAtUtc: string;
}

export interface UpdateScheduleRequest {
  scheduledAtUtc: string;
}

export interface ScheduleResponse {
  id: string;
  linkedInPostId: string;
  recommendedDay: string;
  recommendedTime: string;
  audienceTimezone: string;
  hashtagStrategy: string;
  firstHourEngagementPlan: string;
  status: ScheduleStatus;
  scheduledAtUtc?: string | null;
  publishedAtUtc?: string | null;
  linkedInExternalPostId?: string | null;
  publishErrorDetails?: string | null;
}
