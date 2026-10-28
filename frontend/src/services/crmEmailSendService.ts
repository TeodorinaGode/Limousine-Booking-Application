import { apiRequest } from "./apiClient";
import type { CrmEmailSendResultDto, SendCrmEmailRequest } from "../types/crmEmailSend";

/**
 * PUT, not POST — the resource is the send attempt itself, identified by
 * clientOperationId. Retrying the exact same attempt means calling this
 * again with the exact same id, so the backend can recognize a replay
 * instead of sending twice. A non-2xx response (400/502/503) is a definite
 * outcome; HTTP 202 is not an error — it means the result is still
 * unconfirmed, and apiRequest returns it normally as { status: "Submitting" }.
 */
export function sendCrmEmail(clientOperationId: string, request: SendCrmEmailRequest, accessToken: string): Promise<CrmEmailSendResultDto> {
  return apiRequest<CrmEmailSendResultDto>(`/admin/crm/email/send-operations/${clientOperationId}`, {
    method: "PUT",
    body: request,
    accessToken,
  });
}
