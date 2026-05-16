export interface SalesRevenueReportItem {
  saleUserId: string;
  saleName: string;
  isSaleDeleted: boolean;
  quotationCount: number;
  totalRevenueGross: number;
  totalRevenueNet: number;
}

export interface SalesRevenueReport {
  from: string;
  to: string;
  items: SalesRevenueReportItem[];
  totalQuotationCount: number;
  grandTotalGross: number;
  grandTotalNet: number;
}

export interface SalesRevenueReportParams {
  from: string;
  to: string;
  saleUserId?: string;
}
