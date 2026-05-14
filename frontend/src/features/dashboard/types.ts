export interface QuotationStats {
  totalCount: number;
  draftCount: number;
  sentCount: number;
  confirmedCount: number;
  convertedCount: number;
  cancelledCount: number;
  totalRevenue: number;
  todayRevenue: number;
  from: string;
  to: string;
}
