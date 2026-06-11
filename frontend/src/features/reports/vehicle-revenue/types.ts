export interface VehicleRevenueReportItem {
  vehicleNumber: string;
  companyVehicleRevenue: number;
  externalVehicleRevenue: number;  // âm
}

export interface VehicleRevenueMonthlyPoint {
  month: string;           // "yyyy-MM"
  companyTotal: number;
  externalTotal: number;   // âm
}

export interface VehicleRevenueReport {
  from: string;
  to: string;
  months: number;
  items: VehicleRevenueReportItem[];
  monthlySeries: VehicleRevenueMonthlyPoint[];
  grandTotalCompany: number;
  grandTotalExternal: number;  // âm
}

export interface VehicleRevenueReportParams {
  from: string;
  to: string;
  months?: number;
}
