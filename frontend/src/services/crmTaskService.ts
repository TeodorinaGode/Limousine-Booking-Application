import { apiRequest } from "./apiClient";
import type { PagedResult } from "../types/api";
import type { CreateCrmTaskRequest, CrmTaskDto, CrmTaskSearchParams, UpdateCrmTaskRequest } from "../types/crmTask";

export function getCrmTasks(params: CrmTaskSearchParams, accessToken: string): Promise<PagedResult<CrmTaskDto>> {
  return apiRequest<PagedResult<CrmTaskDto>>("/admin/crm/tasks", {
    accessToken,
    query: {
      search: params.search,
      status: params.status,
      priority: params.priority,
      assignedUserId: params.assignedUserId,
      companyId: params.companyId,
      contactId: params.contactId,
      dealId: params.dealId,
      overdue: params.overdue,
      dueToday: params.dueToday,
      page: params.page,
      pageSize: params.pageSize,
    },
  });
}

export function createCrmTask(data: CreateCrmTaskRequest, accessToken: string): Promise<CrmTaskDto> {
  return apiRequest<CrmTaskDto>("/admin/crm/tasks", { method: "POST", body: data, accessToken });
}

export function updateCrmTask(id: string, data: UpdateCrmTaskRequest, accessToken: string): Promise<CrmTaskDto> {
  return apiRequest<CrmTaskDto>(`/admin/crm/tasks/${id}`, { method: "PUT", body: data, accessToken });
}

export function completeCrmTask(id: string, accessToken: string): Promise<CrmTaskDto> {
  return apiRequest<CrmTaskDto>(`/admin/crm/tasks/${id}/complete`, { method: "PUT", accessToken });
}

export function cancelCrmTask(id: string, accessToken: string): Promise<CrmTaskDto> {
  return apiRequest<CrmTaskDto>(`/admin/crm/tasks/${id}/cancel`, { method: "PUT", accessToken });
}
