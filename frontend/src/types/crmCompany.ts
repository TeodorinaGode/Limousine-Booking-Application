import type { CrmActivityDto } from "./crmActivity";
import type { CrmContactSummary, CrmDealSummary, CrmOwnerSummary, CrmTaskSummary } from "./crmShared";

export const CRM_COMPANY_TYPES = [
  "HotelResort",
  "TravelAgency",
  "CorporateClient",
  "EventOrganizer",
  "WeddingPlanner",
  "TourismPartner",
  "ConciergeService",
  "Other",
] as const;
export type CrmCompanyType = (typeof CRM_COMPANY_TYPES)[number];

export const CRM_LIFECYCLE_STATUSES = [
  "NewProspect",
  "Researching",
  "Contacted",
  "InDiscussion",
  "Qualified",
  "Customer",
  "Inactive",
  "NotInterested",
] as const;
export type CrmLifecycleStatus = (typeof CRM_LIFECYCLE_STATUSES)[number];

export const CRM_PRIORITIES = ["Low", "Normal", "High", "Urgent"] as const;
export type CrmPriority = (typeof CRM_PRIORITIES)[number];

export interface CrmCompanyDto {
  id: string;
  name: string;
  companyType: CrmCompanyType;
  website: string | null;
  domain: string | null;
  industry: string | null;
  country: string | null;
  city: string | null;
  address: string | null;
  phone: string | null;
  email: string | null;
  status: CrmLifecycleStatus;
  priority: CrmPriority;
  assignedOwner: CrmOwnerSummary | null;
  leadSource: string | null;
  lastContactedAt: string | null;
  nextFollowUpAt: string | null;
  notes: string | null;
  isArchived: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CrmCompanyDetailDto {
  company: CrmCompanyDto;
  contacts: CrmContactSummary[];
  deals: CrmDealSummary[];
  openTasks: CrmTaskSummary[];
  recentActivities: CrmActivityDto[];
}

export interface CreateCrmCompanyRequest {
  name: string;
  companyType: CrmCompanyType;
  website?: string | null;
  domain?: string | null;
  industry?: string | null;
  country?: string | null;
  city?: string | null;
  address?: string | null;
  phone?: string | null;
  email?: string | null;
  status: CrmLifecycleStatus;
  priority: CrmPriority;
  assignedOwnerId?: string | null;
  leadSource?: string | null;
  notes?: string | null;
}

export type UpdateCrmCompanyRequest = CreateCrmCompanyRequest;

export interface CrmCompanySearchParams {
  search?: string;
  companyType?: string;
  status?: string;
  priority?: string;
  assignedOwnerId?: string;
  needsFollowUp?: boolean;
  includeArchived?: boolean;
  page?: number;
  pageSize?: number;
}
