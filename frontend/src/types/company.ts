export interface CompanyInfoDto {
  companyName: string;
  tagline: string;
  phone: string;
  email: string;
  address: string;
  website: string;
  openingHours: string;
  emergencyPhone: string | null;
  description: string | null;
  operatingCountryCodes: string[];
  facebookUrl: string | null;
  instagramUrl: string | null;
  whatsAppUrl: string | null;
}
