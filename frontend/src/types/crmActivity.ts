import type { CrmOwnerSummary } from "./crmShared";

export const CRM_ACTIVITY_TYPES = ["Call", "EmailSent", "EmailReceived", "Meeting", "InternalNote", "FollowUp", "Other"] as const;
export type CrmActivityType = (typeof CRM_ACTIVITY_TYPES)[number];

export interface CrmActivityDto {
  id: string;
  activityType: CrmActivityType;
  subject: string;
  description: string | null;
  occurredAt: string;
  loggedBy: CrmOwnerSummary;
  companyId: string | null;
  companyName: string | null;
  contactId: string | null;
  contactName: string | null;
  dealId: string | null;
  dealName: string | null;
  outcome: string | null;
  createdAt: string;
  /** Set only for EmailSent/EmailReceived activities the email sync/send flow created — lets the UI deep-link to the full conversation. */
  emailMessageId: string | null;
  emailThreadId: string | null;
}

export interface CreateCrmActivityRequest {
  activityType: CrmActivityType;
  subject: string;
  description?: string | null;
  occurredAt?: string;
  companyId?: string | null;
  contactId?: string | null;
  dealId?: string | null;
  outcome?: string | null;
}

export interface CrmActivitySearchParams {
  activityType?: string;
  companyId?: string;
  contactId?: string;
  dealId?: string;
  userId?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}
