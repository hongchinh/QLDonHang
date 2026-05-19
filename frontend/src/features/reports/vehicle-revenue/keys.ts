import type { VehicleRevenueReportParams } from './types';

export const vehicleRevenueKeys = {
  all: ['reports', 'vehicle-revenue'] as const,
  list: (p: VehicleRevenueReportParams) => ['reports', 'vehicle-revenue', p] as const,
};
