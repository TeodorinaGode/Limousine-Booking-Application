import type { CrmActivityDto } from "./crmActivity";
import type { CrmCompanySummary, CrmDealSummary, CrmOwnerSummary, CrmTaskSummary } from "./crmShared";
import type { CrmLifecycleStatus } from "./crmCompany";

export interface CrmContactDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  jobTitle: string | null;
  phone: string | null;
  preferredContactMethod: string | null;
  status: CrmLifecycleStatus;
  assignedOwner: CrmOwnerSummary | null;
  lastContactedAt: string | null;
  nextFollowUpAt: string | null;
  notes: string | null;
  isArchived: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CrmContactDetailDto {
  contact: CrmContactDto;
  companies: CrmCompanySummary[];
  deals: CrmDealSummary[];
  openTasks: CrmTaskSummary[];
  recentActivities: CrmActivityDto[];
}

export interface CreateCrmContactRequest {
  firstName: string;
  lastName: string;
  email: string;
  jobTitle?: string | null;
  phone?: string | null;
  preferredContactMethod?: string | null;
  status: CrmLifecycleStatus;
  assignedOwnerId?: string | null;
  notes?: string | null;
  companyId?: string | null;
  isPrimaryAtCompany?: boolean;
}

export interface UpdateCrmContactRequest {
  firstName: string;
  lastName: string;
  email: string;
  jobTitle?: string | null;
  phone?: string | null;
  preferredContactMethod?: string | null;
  status: CrmLifecycleStatus;
  assignedOwnerId?: string | null;
  notes?: string | null;
}

export interface CrmContactSearchParams {
  search?: string;
  status?: string;
  assignedOwnerId?: string;
  companyId?: string;
  needsFollowUp?: boolean;
  includeArchived?: boolean;
  page?: number;
  pageSize?: number;
}
