// Mirrors LinkedInContentManager.Domain.Enums exactly — names match, since the API's
// JsonStringEnumConverter serializes enums by member name, not ordinal.

export enum UserRole {
  Viewer = 'Viewer',
  ContentManager = 'ContentManager',
  Admin = 'Admin',
}

export enum WriterPersona {
  ExecutiveThoughtLeadership = 'ExecutiveThoughtLeadership',
  Practitioner = 'Practitioner',
  ConversationalApproachable = 'ConversationalApproachable',
  FormalAuthoritative = 'FormalAuthoritative',
}

export const WRITER_PERSONA_LABELS: Record<WriterPersona, string> = {
  [WriterPersona.ExecutiveThoughtLeadership]: 'Executive / Thought Leadership',
  [WriterPersona.Practitioner]: 'Practitioner',
  [WriterPersona.ConversationalApproachable]: 'Conversational & Approachable',
  [WriterPersona.FormalAuthoritative]: 'Formal & Authoritative',
};

export enum CampaignStatus {
  Active = 'Active',
  Paused = 'Paused',
  Archived = 'Archived',
}

export enum ExecutionStatus {
  Pending = 'Pending',
  Running = 'Running',
  Completed = 'Completed',
  Failed = 'Failed',
  PartiallyFailed = 'PartiallyFailed',
  Cancelled = 'Cancelled',
}

export enum PostStage {
  Draft = 'Draft',
  Critiqued = 'Critiqued',
  Optimized = 'Optimized',
}

export enum ApprovalStatus {
  Pending = 'Pending',
  Approved = 'Approved',
  Rejected = 'Rejected',
}

export enum ScheduleStatus {
  Recommended = 'Recommended',
  Scheduled = 'Scheduled',
  Published = 'Published',
  Failed = 'Failed',
  Cancelled = 'Cancelled',
}
