import { apiRequest } from "./apiClient";
import type { AccountPreferencesDto, UpdateAccountPreferencesRequest } from "../types/account";

export function getPreferences(accessToken: string): Promise<AccountPreferencesDto> {
  return apiRequest<AccountPreferencesDto>("/account/preferences", { accessToken });
}

export function updatePreferences(data: UpdateAccountPreferencesRequest, accessToken: string): Promise<AccountPreferencesDto> {
  return apiRequest<AccountPreferencesDto>("/account/preferences", { method: "PUT", body: data, accessToken });
}
