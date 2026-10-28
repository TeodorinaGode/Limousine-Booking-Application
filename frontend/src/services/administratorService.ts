import { apiRequest } from "./apiClient";
import type { AdministratorSummaryDto } from "../types/administrator";

export function getAdministrators(accessToken: string): Promise<AdministratorSummaryDto[]> {
  return apiRequest<AdministratorSummaryDto[]>("/admin/users/administrators", { accessToken });
}
