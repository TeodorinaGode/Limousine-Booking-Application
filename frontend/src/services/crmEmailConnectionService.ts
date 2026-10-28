import { apiRequest } from "./apiClient";
import type { AuthorizationUrlDto, EmailConnectionStatusDto } from "../types/crmEmailConnection";

export function getEmailConnectionStatus(accessToken: string): Promise<EmailConnectionStatusDto> {
  return apiRequest<EmailConnectionStatusDto>("/admin/crm/email/connection", { accessToken });
}

export function getEmailAuthorizationUrl(accessToken: string): Promise<AuthorizationUrlDto> {
  return apiRequest<AuthorizationUrlDto>("/admin/crm/email/connection/authorize", { accessToken });
}

export function disconnectEmailAccount(accessToken: string): Promise<void> {
  return apiRequest<void>("/admin/crm/email/connection/disconnect", { method: "POST", accessToken });
}
