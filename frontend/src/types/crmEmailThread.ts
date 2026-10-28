import type { CrmCompanySummary, CrmContactSummary, CrmDealSummary } from "./crmShared";

export interface EmailAddressDto {
  address: string;
  name: string | null;
}

export interface CrmEmailThreadListItemDto {
  id: string;
  subject: string;
  lastMessageAt: string;
  hasUnread: boolean;
  latestFromAddress: string | null;
  latestFromName: string | null;
  associatedCompanyNames: string[];
  associatedContactNames: string[];
  associatedDealNames: string[];
}

export interface CrmEmailAttachmentDto {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
}

export interface CrmEmailMessageDto {
  id: string;
  direction: "Inbound" | "Outbound";
  fromAddress: string;
  fromName: string | null;
  toAddresses: EmailAddressDto[];
  ccAddresses: EmailAddressDto[];
  bccAddresses: EmailAddressDto[];
  subject: string;
  bodyHtml: string | null;
  bodyPlainText: string | null;
  occurredAt: string;
  isRead: boolean;
  attachments: CrmEmailAttachmentDto[];
}

export interface CrmEmailThreadDetailDto {
  id: string;
  subject: string;
  hasUnread: boolean;
  messages: CrmEmailMessageDto[];
  companies: CrmCompanySummary[];
  contacts: CrmContactSummary[];
  deals: CrmDealSummary[];
}

export interface CrmEmailThreadSearchParams {
  search?: string;
  companyId?: string;
  contactId?: string;
  dealId?: string;
  unreadOnly?: boolean;
  page?: number;
  pageSize?: number;
}
