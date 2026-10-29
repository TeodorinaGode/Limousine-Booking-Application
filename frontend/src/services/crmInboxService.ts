import { apiRequest } from "./apiClient";
import type { PagedResult } from "../types/api";
import type { CrmEmailThreadDetailDto, CrmEmailThreadListItemDto, CrmEmailThreadSearchParams } from "../types/crmEmailThread";

export function getCrmEmailThreads(params: CrmEmailThreadSearchParams, accessToken: string): Promise<PagedResult<CrmEmailThreadListItemDto>> {
  return apiRequest<PagedResult<CrmEmailThreadListItemDto>>("/admin/crm/email/threads", {
    accessToken,
    query: {
      search: params.search,
      companyId: params.companyId,
      contactId: params.contactId,
      dealId: params.dealId,
      unreadOnly: params.unreadOnly,
      page: params.page,
      pageSize: params.pageSize,
    },
  });
}

export function getCrmEmailThreadById(id: string, accessToken: string): Promise<CrmEmailThreadDetailDto> {
  return apiRequest<CrmEmailThreadDetailDto>(`/admin/crm/email/threads/${id}`, { accessToken });
}

/** "Remove from CRM" — hides the thread from the Inbox list only. The real Gmail mailbox is untouched. */
export function removeCrmEmailThread(id: string, accessToken: string): Promise<void> {
  return apiRequest<void>(`/admin/crm/email/threads/${id}`, { method: "DELETE", accessToken });
}

export function markCrmEmailThreadRead(id: string, accessToken: string): Promise<CrmEmailThreadDetailDto> {
  return apiRequest<CrmEmailThreadDetailDto>(`/admin/crm/email/threads/${id}/read`, { method: "PUT", accessToken });
}

export function markCrmEmailThreadUnread(id: string, accessToken: string): Promise<CrmEmailThreadDetailDto> {
  return apiRequest<CrmEmailThreadDetailDto>(`/admin/crm/email/threads/${id}/unread`, { method: "PUT", accessToken });
}

export function linkCrmEmailThreadCompany(id: string, companyId: string, accessToken: string): Promise<CrmEmailThreadDetailDto> {
  return apiRequest<CrmEmailThreadDetailDto>(`/admin/crm/email/threads/${id}/companies`, { method: "POST", body: { companyId }, accessToken });
}

export function unlinkCrmEmailThreadCompany(id: string, companyId: string, accessToken: string): Promise<CrmEmailThreadDetailDto> {
  return apiRequest<CrmEmailThreadDetailDto>(`/admin/crm/email/threads/${id}/companies/${companyId}`, { method: "DELETE", accessToken });
}

export function linkCrmEmailThreadContact(id: string, contactId: string, accessToken: string): Promise<CrmEmailThreadDetailDto> {
  return apiRequest<CrmEmailThreadDetailDto>(`/admin/crm/email/threads/${id}/contacts`, { method: "POST", body: { contactId }, accessToken });
}

export function unlinkCrmEmailThreadContact(id: string, contactId: string, accessToken: string): Promise<CrmEmailThreadDetailDto> {
  return apiRequest<CrmEmailThreadDetailDto>(`/admin/crm/email/threads/${id}/contacts/${contactId}`, { method: "DELETE", accessToken });
}

export function linkCrmEmailThreadDeal(id: string, dealId: string, accessToken: string): Promise<CrmEmailThreadDetailDto> {
  return apiRequest<CrmEmailThreadDetailDto>(`/admin/crm/email/threads/${id}/deals`, { method: "POST", body: { dealId }, accessToken });
}

export function unlinkCrmEmailThreadDeal(id: string, dealId: string, accessToken: string): Promise<CrmEmailThreadDetailDto> {
  return apiRequest<CrmEmailThreadDetailDto>(`/admin/crm/email/threads/${id}/deals/${dealId}`, { method: "DELETE", accessToken });
}

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000/api";

/**
 * The attachment endpoint requires a bearer token, so a plain `<a href>`
 * can't hit it directly — this fetches the bytes with the Authorization
 * header attached, then hands the browser a transient blob URL to save,
 * same approach as every other authenticated-file-download flow needs.
 */
export async function downloadCrmEmailAttachment(messageId: string, attachmentId: string, fileName: string, accessToken: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/admin/crm/email/messages/${messageId}/attachments/${attachmentId}`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  if (!response.ok) throw new Error("Failed to download the attachment.");

  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}
