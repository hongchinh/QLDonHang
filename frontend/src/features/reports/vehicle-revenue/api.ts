import { apiGet } from '@/lib/api-client';
import type { VehicleRevenueReport, VehicleRevenueReportParams } from './types';

export const vehicleRevenueApi = {
  get: (params: VehicleRevenueReportParams) =>
    apiGet<VehicleRevenueReport>('/reports/vehicle-revenue', params),
};
