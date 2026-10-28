export interface CrmEmailTemplateDto {
  id: string;
  name: string;
  subject: string;
  bodyHtml: string;
  category: string;
  language: string;
  isArchived: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCrmEmailTemplateRequest {
  name: string;
  subject: string;
  bodyHtml: string;
  category?: string | null;
  language?: string | null;
}

export interface UpdateCrmEmailTemplateRequest {
  name: string;
  subject: string;
  bodyHtml: string;
  category?: string | null;
  language?: string | null;
}

export interface CrmEmailTemplateSearchParams {
  search?: string;
  category?: string;
  includeArchived?: boolean;
  page?: number;
  pageSize?: number;
}

export interface CrmEmailTemplateRenderResultDto {
  subject: string;
  bodyHtml: string;
  unresolvedVariables: string[];
}
