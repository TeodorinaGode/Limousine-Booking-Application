import type { EmailAddressDto } from "./crmEmailThread";

export interface SendCrmEmailRequest {
  threadId?: string | null;
  toAddresses: EmailAddressDto[];
  ccAddresses?: EmailAddressDto[];
  bccAddresses?: EmailAddressDto[];
  subject: string;
  bodyHtml: string;
  templateId?: string | null;
  companyId?: string | null;
  contactId?: string | null;
  dealId?: string | null;
}

/** Status mirrors the backend's EmailSendStatus exactly — "Accepted" is the only status that means the provider actually confirmed the send. */
export interface CrmEmailSendResultDto {
  status: "Pending" | "Submitting" | "Accepted" | "Failed";
  messageId: string | null;
}
