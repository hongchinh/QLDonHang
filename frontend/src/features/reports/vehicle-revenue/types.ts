export interface VehicleRevenueReportItem {
  vehicleNumber: string;
  quotationCount: number;
  totalRevenueGross: number;
  totalRevenueNet: number;
}

export interface VehicleRevenueMonthlyValue {
  vehicleNumber: string;
  totalRevenueGross: number;
}

export interface VehicleRevenueMonthlyPoint {
  month: string;
  values: VehicleRevenueMonthlyValue[];
}

export interface VehicleRevenueReport {
  from: string;
  to: string;
  months: number;
  topVehicles: number;
  items: VehicleRevenueReportItem[];
  chartVehicles: string[];
  monthlySeries: VehicleRevenueMonthlyPoint[];
  totalQuotationCount: number;
  grandTotalGross: number;
  grandTotalNet: number;
}

export interface VehicleRevenueReportParams {
  from: string;
  to: string;
  months?: number;
  topVehicles?: number;
}
