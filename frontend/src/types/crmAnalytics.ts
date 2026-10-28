import type { CrmActivityDto } from "./crmActivity";

export interface CrmDashboardDto {
  totalCompanies: number;
  totalContacts: number;
  companiesContactedThisMonth: number;
  contactsAndCompaniesNeedingFollowUp: number;
  openDeals: number;
  wonDeals: number;
  lostDeals: number;
  openPipelineValue: number;
  weightedPipelineValue: number;
  currency: string;
  tasksDueToday: number;
  overdueTasks: number;
  upcomingFollowUps: number;
  recentActivities: CrmActivityDto[];
}

export interface CrmDealsByStageItemDto {
  stageName: string;
  sortOrder: number;
  dealCount: number;
  totalValue: number;
}

export interface CrmReportSummaryDto {
  dateFrom: string;
  dateTo: string;
  newCompanies: number;
  newContacts: number;
  activitiesLogged: number;
  tasksCompleted: number;
  dealsWon: number;
  dealsLost: number;
  wonValue: number;
  currency: string;
  conversionRatePercent: number | null;
  openDealsByStage: CrmDealsByStageItemDto[];
}
