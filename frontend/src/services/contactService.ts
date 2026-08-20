import { apiRequest } from "./apiClient";
import type { ContactFormRequest } from "../types/contact";

export function submitContactForm(data: ContactFormRequest): Promise<{ message: string }> {
  return apiRequest<{ message: string }>("/public/contact", { method: "POST", body: data });
}
