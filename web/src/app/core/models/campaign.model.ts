import { CampaignStatus, WriterPersona } from './enums.model';

export interface CreateCampaignRequest {
  name: string;
  nicheId: string;
  defaultPersona: WriterPersona;
}

export interface UpdateCampaignRequest {
  name: string;
  defaultPersona: WriterPersona;
  status: CampaignStatus;
}

export interface CampaignResponse {
  id: string;
  name: string;
  nicheId: string;
  nicheName: string;
  defaultPersona: WriterPersona;
  ownerUserId: string;
  status: CampaignStatus;
  contentTopicCount: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}
