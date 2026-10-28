export interface CrmOwnerSummary {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
}

export interface CrmCompanySummary {
  id: string;
  name: string;
  companyType: string;
}

export interface CrmContactSummary {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  jobTitle: string | null;
  isPrimary?: boolean;
}

export interface CrmDealSummary {
  id: string;
  name: string;
  value: number;
  currency: string;
  stageId: string;
  stageName: string;
  probability: number;
  isWon: boolean;
  isLost: boolean;
}

export interface CrmTaskSummary {
  id: string;
  title: string;
  dueDate: string;
  status: string;
  priority: string;
}
