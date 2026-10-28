import type { CrmOwnerSummary } from "./crmShared";

export const CRM_TASK_PRIORITIES = ["Low", "Normal", "High", "Urgent"] as const;
export type CrmTaskPriority = (typeof CRM_TASK_PRIORITIES)[number];

export const CRM_TASK_STATUSES = ["ToDo", "InProgress", "Completed", "Cancelled"] as const;
export type CrmTaskStatus = (typeof CRM_TASK_STATUSES)[number];

export interface CrmTaskDto {
  id: string;
  title: string;
  description: string | null;
  dueDate: string;
  dueTime: string | null;
  assignedUser: CrmOwnerSummary;
  priority: CrmTaskPriority;
  status: CrmTaskStatus;
  companyId: string | null;
  companyName: string | null;
  contactId: string | null;
  contactName: string | null;
  dealId: string | null;
  dealName: string | null;
  completedAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCrmTaskRequest {
  title: string;
  description?: string | null;
  dueDate: string;
  dueTime?: string | null;
  assignedUserId: string;
  priority: CrmTaskPriority;
  companyId?: string | null;
  contactId?: string | null;
  dealId?: string | null;
}

export type UpdateCrmTaskRequest = CreateCrmTaskRequest;

export interface CrmTaskSearchParams {
  search?: string;
  status?: string;
  priority?: string;
  assignedUserId?: string;
  companyId?: string;
  contactId?: string;
  dealId?: string;
  overdue?: boolean;
  dueToday?: boolean;
  page?: number;
  pageSize?: number;
}
