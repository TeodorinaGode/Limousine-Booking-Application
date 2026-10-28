import { apiRequest } from "./apiClient";
import type { PagedResult } from "../types/api";
import type { CreateCrmActivityRequest, CrmActivityDto, CrmActivitySearchParams } from "../types/crmActivity";

export function getCrmActivities(params: CrmActivitySearchParams, accessToken: string): Promise<PagedResult<CrmActivityDto>> {
  return apiRequest<PagedResult<CrmActivityDto>>("/admin/crm/activities", {
    accessToken,
    query: {
      activityType: params.activityType,
      companyId: params.companyId,
      contactId: params.contactId,
      dealId: params.dealId,
      userId: params.userId,
      from: params.from,
      to: params.to,
      page: params.page,
      pageSize: params.pageSize,
    },
  });
}

export function createCrmActivity(data: CreateCrmActivityRequest, accessToken: string): Promise<CrmActivityDto> {
  return apiRequest<CrmActivityDto>("/admin/crm/activities", { method: "POST", body: data, accessToken });
}
