import { apiRequest } from "./apiClient";
import type { PagedResult } from "../types/api";
import type {
  CreateCrmContactRequest,
  CrmContactDetailDto,
  CrmContactDto,
  CrmContactSearchParams,
  UpdateCrmContactRequest,
} from "../types/crmContact";

export function getCrmContacts(params: CrmContactSearchParams, accessToken: string): Promise<PagedResult<CrmContactDto>> {
  return apiRequest<PagedResult<CrmContactDto>>("/admin/crm/contacts", {
    accessToken,
    query: {
      search: params.search,
      status: params.status,
      assignedOwnerId: params.assignedOwnerId,
      companyId: params.companyId,
      needsFollowUp: params.needsFollowUp,
      includeArchived: params.includeArchived,
      page: params.page,
      pageSize: params.pageSize,
    },
  });
}

export function getCrmContactById(id: string, accessToken: string): Promise<CrmContactDetailDto> {
  return apiRequest<CrmContactDetailDto>(`/admin/crm/contacts/${id}`, { accessToken });
}

export function createCrmContact(data: CreateCrmContactRequest, accessToken: string): Promise<CrmContactDto> {
  return apiRequest<CrmContactDto>("/admin/crm/contacts", { method: "POST", body: data, accessToken });
}

export function updateCrmContact(id: string, data: UpdateCrmContactRequest, accessToken: string): Promise<CrmContactDto> {
  return apiRequest<CrmContactDto>(`/admin/crm/contacts/${id}`, { method: "PUT", body: data, accessToken });
}

export function archiveCrmContact(id: string, accessToken: string): Promise<CrmContactDto> {
  return apiRequest<CrmContactDto>(`/admin/crm/contacts/${id}/archive`, { method: "PUT", accessToken });
}

export function unarchiveCrmContact(id: string, accessToken: string): Promise<CrmContactDto> {
  return apiRequest<CrmContactDto>(`/admin/crm/contacts/${id}/unarchive`, { method: "PUT", accessToken });
}

export function recordCrmContactContacted(id: string, contactedAt: string, accessToken: string): Promise<CrmContactDto> {
  return apiRequest<CrmContactDto>(`/admin/crm/contacts/${id}/contacted`, {
    method: "PUT",
    body: { contactedAt },
    accessToken,
  });
}

export function scheduleCrmContactFollowUp(id: string, followUpAt: string | null, accessToken: string): Promise<CrmContactDto> {
  return apiRequest<CrmContactDto>(`/admin/crm/contacts/${id}/follow-up`, {
    method: "PUT",
    body: { followUpAt },
    accessToken,
  });
}
