import { apiRequest } from "./apiClient";
import type { PagedResult } from "../types/api";
import type {
  CreateCrmCompanyRequest,
  CrmCompanyDetailDto,
  CrmCompanyDto,
  CrmCompanySearchParams,
  UpdateCrmCompanyRequest,
} from "../types/crmCompany";

export function getCrmCompanies(params: CrmCompanySearchParams, accessToken: string): Promise<PagedResult<CrmCompanyDto>> {
  return apiRequest<PagedResult<CrmCompanyDto>>("/admin/crm/companies", {
    accessToken,
    query: {
      search: params.search,
      companyType: params.companyType,
      status: params.status,
      priority: params.priority,
      assignedOwnerId: params.assignedOwnerId,
      needsFollowUp: params.needsFollowUp,
      includeArchived: params.includeArchived,
      page: params.page,
      pageSize: params.pageSize,
    },
  });
}

export function getCrmCompanyById(id: string, accessToken: string): Promise<CrmCompanyDetailDto> {
  return apiRequest<CrmCompanyDetailDto>(`/admin/crm/companies/${id}`, { accessToken });
}

export function createCrmCompany(data: CreateCrmCompanyRequest, accessToken: string): Promise<CrmCompanyDto> {
  return apiRequest<CrmCompanyDto>("/admin/crm/companies", { method: "POST", body: data, accessToken });
}

export function updateCrmCompany(id: string, data: UpdateCrmCompanyRequest, accessToken: string): Promise<CrmCompanyDto> {
  return apiRequest<CrmCompanyDto>(`/admin/crm/companies/${id}`, { method: "PUT", body: data, accessToken });
}

export function archiveCrmCompany(id: string, accessToken: string): Promise<CrmCompanyDto> {
  return apiRequest<CrmCompanyDto>(`/admin/crm/companies/${id}/archive`, { method: "PUT", accessToken });
}

export function unarchiveCrmCompany(id: string, accessToken: string): Promise<CrmCompanyDto> {
  return apiRequest<CrmCompanyDto>(`/admin/crm/companies/${id}/unarchive`, { method: "PUT", accessToken });
}

export function recordCrmCompanyContacted(id: string, contactedAt: string, accessToken: string): Promise<CrmCompanyDto> {
  return apiRequest<CrmCompanyDto>(`/admin/crm/companies/${id}/contacted`, {
    method: "PUT",
    body: { contactedAt },
    accessToken,
  });
}

export function scheduleCrmCompanyFollowUp(id: string, followUpAt: string | null, accessToken: string): Promise<CrmCompanyDto> {
  return apiRequest<CrmCompanyDto>(`/admin/crm/companies/${id}/follow-up`, {
    method: "PUT",
    body: { followUpAt },
    accessToken,
  });
}

export function linkCrmCompanyContact(companyId: string, contactId: string, isPrimary: boolean, accessToken: string): Promise<CrmCompanyDto> {
  return apiRequest<CrmCompanyDto>(`/admin/crm/companies/${companyId}/contacts`, {
    method: "POST",
    body: { contactId, isPrimary },
    accessToken,
  });
}

export function unlinkCrmCompanyContact(companyId: string, contactId: string, accessToken: string): Promise<CrmCompanyDto> {
  return apiRequest<CrmCompanyDto>(`/admin/crm/companies/${companyId}/contacts/${contactId}`, {
    method: "DELETE",
    accessToken,
  });
}
