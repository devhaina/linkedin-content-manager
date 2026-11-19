import { ApprovalStatus, ExecutionStatus, PostStage, ScheduleStatus, WriterPersona } from './enums.model';

export interface GenerateContentRequest {
  campaignId: string;
  topic: string;
  personaOverride?: WriterPersona | null;
}

export interface GenerateContentResponse {
  executionId: string;
  status: ExecutionStatus;
}

export interface RegenerateContentRequest {
  personaOverride?: WriterPersona | null;
}

export interface ResearchBriefDto {
  trendingContentAngles: string[];
  trendingHashtags: string[];
  trendingHooks: string[];
  sourceUrls: string[];
  rawOutput: string;
}

export interface ContentReviewDto {
  score: number;
  hookEvaluation: string;
  storytellingEvaluation: string;
  engagementPotential: string;
  ctaEffectiveness: string;
  toneReadabilityAssessment: string;
  guardrailViolations: string[];
  recommendations: string[];
  createdAtUtc: string;
}

export interface SchedulingBriefDto {
  recommendedDay: string;
  recommendedTime: string;
  audienceTimezone: string;
  hashtagStrategy: string;
  firstHourEngagementPlan: string;
  status: ScheduleStatus;
  scheduledAtUtc?: string | null;
  publishedAtUtc?: string | null;
  linkedInExternalPostId?: string | null;
}

export interface ContentDetailResponse {
  executionId: string;
  contentTopicId: string;
  topic: string;
  status: ExecutionStatus;
  retryCount: number;
  errorDetails?: string | null;
  persona: WriterPersona;
  linkedInPostId?: string | null;
  draftContent?: string | null;
  optimizedContent?: string | null;
  stage?: PostStage | null;
  hashtags: string[];
  research?: ResearchBriefDto | null;
  reviews: ContentReviewDto[];
  approvalStatus?: ApprovalStatus | null;
  scheduling?: SchedulingBriefDto | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface ContentHistoryItemResponse {
  executionId: string;
  campaignId: string;
  campaignName: string;
  topic: string;
  status: ExecutionStatus;
  critiqueScore?: number | null;
  approvalStatus?: ApprovalStatus | null;
  scheduleStatus?: ScheduleStatus | null;
  createdAtUtc: string;
}

export interface RejectContentRequest {
  reason: string;
}

export interface EditContentRequest {
  editedContent: string;
}

export interface ApprovalActionResponse {
  linkedInPostId: string;
  status: ApprovalStatus;
  reviewedAtUtc: string;
}

export interface EditContentResponse {
  linkedInPostId: string;
  optimizedContent: string;
  wordCount: number;
}
