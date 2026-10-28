import { apiRequest } from "./apiClient";
import type { PagedResult } from "../types/api";
import type {
  CreateCrmDealRequest,
  CrmDealDetailDto,
  CrmDealDto,
  CrmDealSearchParams,
  CrmPipelineBoardDto,
  MoveCrmDealStageRequest,
  UpdateCrmDealRequest,
} from "../types/crmDeal";

export function getCrmDeals(params: CrmDealSearchParams, accessToken: string): Promise<PagedResult<CrmDealDto>> {
  return apiRequest<PagedResult<CrmDealDto>>("/admin/crm/deals", {
    accessToken,
    query: {
      search: params.search,
      stageId: params.stageId,
      companyId: params.companyId,
      ownerId: params.ownerId,
      isOpen: params.isOpen,
      includeArchived: params.includeArchived,
      page: params.page,
      pageSize: params.pageSize,
    },
  });
}

export function getCrmDealBoard(accessToken: string): Promise<CrmPipelineBoardDto> {
  return apiRequest<CrmPipelineBoardDto>("/admin/crm/deals/board", { accessToken });
}

export function getCrmDealById(id: string, accessToken: string): Promise<CrmDealDetailDto> {
  return apiRequest<CrmDealDetailDto>(`/admin/crm/deals/${id}`, { accessToken });
}

export function createCrmDeal(data: CreateCrmDealRequest, accessToken: string): Promise<CrmDealDto> {
  return apiRequest<CrmDealDto>("/admin/crm/deals", { method: "POST", body: data, accessToken });
}

export function updateCrmDeal(id: string, data: UpdateCrmDealRequest, accessToken: string): Promise<CrmDealDto> {
  return apiRequest<CrmDealDto>(`/admin/crm/deals/${id}`, { method: "PUT", body: data, accessToken });
}

export function moveCrmDealStage(id: string, data: MoveCrmDealStageRequest, accessToken: string): Promise<CrmDealDto> {
  return apiRequest<CrmDealDto>(`/admin/crm/deals/${id}/stage`, { method: "PUT", body: data, accessToken });
}

export function archiveCrmDeal(id: string, accessToken: string): Promise<CrmDealDto> {
  return apiRequest<CrmDealDto>(`/admin/crm/deals/${id}/archive`, { method: "PUT", accessToken });
}

export function unarchiveCrmDeal(id: string, accessToken: string): Promise<CrmDealDto> {
  return apiRequest<CrmDealDto>(`/admin/crm/deals/${id}/unarchive`, { method: "PUT", accessToken });
}

export function linkCrmDealContact(dealId: string, contactId: string, accessToken: string): Promise<CrmDealDto> {
  return apiRequest<CrmDealDto>(`/admin/crm/deals/${dealId}/contacts`, { method: "POST", body: { contactId }, accessToken });
}

export function unlinkCrmDealContact(dealId: string, contactId: string, accessToken: string): Promise<CrmDealDto> {
  return apiRequest<CrmDealDto>(`/admin/crm/deals/${dealId}/contacts/${contactId}`, { method: "DELETE", accessToken });
}
