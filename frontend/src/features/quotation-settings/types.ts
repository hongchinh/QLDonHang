export type RevenueReportingDateField = 'QuotationDate' | 'ConfirmedAt' | 'AccountingConfirmedAt';

export interface QuotationSystemSettings {
  revenueReportingDateField: RevenueReportingDateField;
  updatedAt: string;
  updatedByName?: string;
}

export interface UpdateQuotationSystemSettingsRequest {
  revenueReportingDateField: RevenueReportingDateField;
}
