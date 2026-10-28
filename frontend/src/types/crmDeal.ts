import type { CrmActivityDto } from "./crmActivity";
import type { CrmCompanySummary, CrmContactSummary, CrmOwnerSummary, CrmTaskSummary } from "./crmShared";

export interface CrmPipelineStageSummaryDto {
  id: string;
  name: string;
  sortOrder: number;
  isWon: boolean;
  isLost: boolean;
  defaultProbability: number;
}

export interface CrmDealDto {
  id: string;
  name: string;
  company: CrmCompanySummary;
  description: string | null;
  dealType: string | null;
  value: number;
  currency: string;
  pipelineId: string;
  stageId: string;
  stageName: string;
  isWon: boolean;
  isLost: boolean;
  probability: number;
  expectedCloseDate: string | null;
  owner: CrmOwnerSummary | null;
  nextAction: string | null;
  closedAt: string | null;
  lossReason: string | null;
  notes: string | null;
  isArchived: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CrmDealDetailDto {
  deal: CrmDealDto;
  contacts: CrmContactSummary[];
  openTasks: CrmTaskSummary[];
  recentActivities: CrmActivityDto[];
}

export interface CrmPipelineBoardColumnDto {
  stage: CrmPipelineStageSummaryDto;
  deals: CrmDealDto[];
}

export interface CrmPipelineBoardDto {
  pipelineId: string;
  pipelineName: string;
  columns: CrmPipelineBoardColumnDto[];
}

export interface CreateCrmDealRequest {
  name: string;
  companyId: string;
  description?: string | null;
  dealType?: string | null;
  value: number;
  currency: string;
  stageId?: string | null;
  expectedCloseDate?: string | null;
  ownerId?: string | null;
  nextAction?: string | null;
  notes?: string | null;
}

export interface UpdateCrmDealRequest {
  name: string;
  description?: string | null;
  dealType?: string | null;
  value: number;
  currency: string;
  expectedCloseDate?: string | null;
  ownerId?: string | null;
  nextAction?: string | null;
  notes?: string | null;
}

export interface MoveCrmDealStageRequest {
  stageId: string;
  lossReason?: string | null;
}

export interface CrmDealSearchParams {
  search?: string;
  stageId?: string;
  companyId?: string;
  ownerId?: string;
  isOpen?: boolean;
  includeArchived?: boolean;
  page?: number;
  pageSize?: number;
}
