export interface ContactFormRequest {
  name: string;
  email: string;
  phone?: string;
  subject: string;
  message: string;
  preferredContactMethod?: "Phone" | "Email";
  preferredDate?: string;
}
