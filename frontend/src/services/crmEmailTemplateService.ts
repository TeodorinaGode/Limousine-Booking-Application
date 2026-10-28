import { apiRequest } from "./apiClient";
import type { PagedResult } from "../types/api";
import type {
  CreateCrmEmailTemplateRequest,
  CrmEmailTemplateDto,
  CrmEmailTemplateRenderResultDto,
  CrmEmailTemplateSearchParams,
  UpdateCrmEmailTemplateRequest,
} from "../types/crmEmailTemplate";

export function getCrmEmailTemplates(params: CrmEmailTemplateSearchParams, accessToken: string): Promise<PagedResult<CrmEmailTemplateDto>> {
  return apiRequest<PagedResult<CrmEmailTemplateDto>>("/admin/crm/email/templates", {
    accessToken,
    query: { search: params.search, category: params.category, includeArchived: params.includeArchived, page: params.page, pageSize: params.pageSize },
  });
}

export function createCrmEmailTemplate(data: CreateCrmEmailTemplateRequest, accessToken: string): Promise<CrmEmailTemplateDto> {
  return apiRequest<CrmEmailTemplateDto>("/admin/crm/email/templates", { method: "POST", body: data, accessToken });
}

export function updateCrmEmailTemplate(id: string, data: UpdateCrmEmailTemplateRequest, accessToken: string): Promise<CrmEmailTemplateDto> {
  return apiRequest<CrmEmailTemplateDto>(`/admin/crm/email/templates/${id}`, { method: "PUT", body: data, accessToken });
}

export function archiveCrmEmailTemplate(id: string, accessToken: string): Promise<CrmEmailTemplateDto> {
  return apiRequest<CrmEmailTemplateDto>(`/admin/crm/email/templates/${id}/archive`, { method: "PUT", accessToken });
}

export function unarchiveCrmEmailTemplate(id: string, accessToken: string): Promise<CrmEmailTemplateDto> {
  return apiRequest<CrmEmailTemplateDto>(`/admin/crm/email/templates/${id}/unarchive`, { method: "PUT", accessToken });
}

export function renderCrmEmailTemplate(
  id: string,
  context: { companyId?: string | null; contactId?: string | null; dealId?: string | null },
  accessToken: string
): Promise<CrmEmailTemplateRenderResultDto> {
  return apiRequest<CrmEmailTemplateRenderResultDto>(`/admin/crm/email/templates/${id}/render`, {
    accessToken,
    query: { companyId: context.companyId ?? undefined, contactId: context.contactId ?? undefined, dealId: context.dealId ?? undefined },
  });
}
